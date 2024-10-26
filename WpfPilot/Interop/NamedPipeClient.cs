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
	public NamedPipeClient(string pipeName, Func<int?> getProcessExitCode, Action reinject)
	{
		PipeName = pipeName;
		Pipe = new NamedPipeClientStream(".", pipeName, PipeDirection.InOut);
		GetProcessExitCode = getProcessExitCode;
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

	public dynamic? GetResponse(object command, bool returnOnCleanExit = false)
	{
		_ = command ?? throw new ArgumentNullException(nameof(command));

		var exitCode = GetProcessExitCode();
		ProcessUtility.ThrowOnAppErrorCode(exitCode, PipeName);

		if (exitCode == 0)
		{
			if (returnOnCleanExit)
				return null;
			else
				throw new InvalidOperationException($"App has exited.");
		}

		string? rawMessage = null;
		Retry.With(() =>
		{
			ProcessUtility.ThrowOnAppErrorCode(GetProcessExitCode(), PipeName);
			if (returnOnCleanExit && GetProcessExitCode() == 0)
				return;

			// Connection was severed and we need to reconnect.
			// This can happen if the app has multiple screens, for example a login window that then launches the main window.
			if (!Pipe.IsConnected)
			{
				Log.Info("Pipe is reconnecting.");
				Reinject();

				if (returnOnCleanExit && GetProcessExitCode() == 0)
					return;

				Pipe.Connect((int) TimeSpan.FromSeconds(10).TotalMilliseconds);
				ProcessUtility.ThrowOnAppErrorCode(GetProcessExitCode(), PipeName);
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

			ProcessUtility.ThrowOnAppErrorCode(GetProcessExitCode(), PipeName);
			if (returnOnCleanExit && GetProcessExitCode() == 0)
				return;

			var reader = new StreamReader(Pipe);
			rawMessage = TimeoutAfter<string?>(reader.ReadLineAsync(), TimeSpan.FromSeconds(10))
				.ConfigureAwait(false)
				.GetAwaiter()
				.GetResult();

			if (returnOnCleanExit && GetProcessExitCode() == 0)
				return;

			if (rawMessage == null)
				throw new InvalidOperationException("Failed to read response from the app. This is usually caused by the app crashing.");
		}, retryIntervalMs: 1000, retryCount: 20);

		if (rawMessage == null)
			return null;

		dynamic response = MessagePacker.Unpack(rawMessage);
		if (PropInfo.HasProperty(response, "Error"))
			throw new InvalidOperationException($"An error response was received from the app.\n{response.Error}");

		ProcessUtility.ThrowOnAppErrorCode(GetProcessExitCode(), PipeName);

		return response;
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
	private Func<int?> GetProcessExitCode { get; }
}
