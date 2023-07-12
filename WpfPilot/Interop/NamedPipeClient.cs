namespace WpfPilot.Interop;

using System;
using System.IO;
using System.IO.Pipes;
using System.Threading;
using System.Threading.Tasks;
using WpfPilot.Utility;
using WpfPilot.Utility.WpfUtility;
using static WpfPilot.Utility.Retry;

internal sealed class NamedPipeClient : IDisposable
{
	public NamedPipeClient(string pipeName, Func<bool> hasProcessExited, Action reinject)
	{
		PipeName = pipeName;
		Pipe = new NamedPipeClientStream(".", pipeName, PipeDirection.InOut);
		HasProcessExited = hasProcessExited;
		Reinject = reinject;
		Retry.With(() =>
		{
			try
			{
				Pipe.Connect((int) TimeSpan.FromSeconds(10).TotalMilliseconds);
			}
			catch (TimeoutException)
			{
				Reinject();
			}
		}, retryIntervalMs: 1, retryCount: 10);
	}

	public void Dispose()
	{
		Pipe.Dispose();
	}

	public dynamic GetResponse(object command)
	{
		_ = command ?? throw new ArgumentNullException(nameof(command));

		if (HasProcessExited())
			throw new InvalidOperationException($"App has exited.{ExceptionLog.ReadLog("")}");

		string? rawMessage = null;
		Retry.With(() =>
		{
			ThrowIfAppExited();

			// Connection was severed and we need to reconnect.
			// This can happen if the app has multiple screens, for example a login window that then launches the main window.
			if (!Pipe.IsConnected)
			{
				Log.Info("Pipe is reconnecting.");
				Reinject();
				Pipe.Connect((int) TimeSpan.FromSeconds(10).TotalMilliseconds);
				ThrowIfAppExited();
			}

			var writer = new StreamWriter(Pipe);
			string serialized = MessagePacker.Pack(command);
			var writeResponseAsync = async () =>
			{
				await writer.WriteAsync(serialized);
				await writer.WriteAsync('\n');
				await writer.FlushAsync();
				return true;
			};
			var writeResponseTask = writeResponseAsync();
			TimeoutAfter<bool>(writeResponseTask, TimeSpan.FromSeconds(10))
				.ConfigureAwait(false)
				.GetAwaiter()
				.GetResult();

			ThrowIfAppExited();

			var reader = new StreamReader(Pipe);
			rawMessage = TimeoutAfter<string?>(reader.ReadLineAsync(), TimeSpan.FromSeconds(10))
				.ConfigureAwait(false)
				.GetAwaiter()
				.GetResult();

			if (rawMessage == null)
				throw new InvalidOperationException("Failed to read response from the app. This is usually caused by the app crashing.");
		}, retryIntervalMs: 1000, retryCount: 20);

		dynamic response = MessagePacker.Unpack(rawMessage);
		if (PropInfo.HasProperty(response, "Error"))
			throw new InvalidOperationException($"An error response was received from the app.\n{response.Error}");

		return response;
	}

	private void ThrowIfAppExited()
	{
		if (!HasProcessExited())
			return;

		var exceptionMessage = "App has unexpectedly exited.";

		var crashLogExists = Wait.Until(() => File.Exists($"{PipeName}-crash.txt"), timeoutMs: 500, retryIntervalMs: 100);
		if (crashLogExists)
		{
			var lastUnhandledException = ExceptionLog.ReadLog($"{PipeName}-crash.txt");
			if (lastUnhandledException.Length != 0)
				exceptionMessage += $" Last unhandled exception:\n{lastUnhandledException}";
		}

		throw new RetryException(exceptionMessage);
	}

	// https://stackoverflow.com/a/22078975
	private static async Task<TResult> TimeoutAfter<TResult>(Task<TResult> task, TimeSpan timeout)
	{
		using var timeoutCancellationTokenSource = new CancellationTokenSource();
		var completedTask = await Task.WhenAny(task, Task.Delay(timeout, timeoutCancellationTokenSource.Token));
		if (completedTask == task)
		{
			timeoutCancellationTokenSource.Cancel();
			return await task; // Very important in order to propagate exceptions
		}
		else
		{
			throw new TimeoutException("The operation has timed out.");
		}
	}

	private Action Reinject { get; }
	private string PipeName { get; }
	private NamedPipeClientStream Pipe { get; }
	private Func<bool> HasProcessExited { get; }
}
