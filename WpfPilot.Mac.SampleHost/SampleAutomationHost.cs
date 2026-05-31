namespace WpfPilot.Mac.SampleHost;

using System;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// A stand-in for the application's real automation host (e.g. Logos' <c>E2EAutomationHost</c>). It
/// exposes the same shapes the macOS driver must handle: sync scalar methods, an <see cref="int"/>
/// argument (to prove primitive types are not widened to <see cref="long"/> over the wire), async
/// <see cref="Task{TResult}"/> methods, and a deliberately slow method for timeout coverage.
/// </summary>
public sealed class SampleAutomationHost
{
	public string Ping() => "pong";

	public int Add(int a, int b) => a + b;

	public string Describe(int count) => $"int:{count}:{count.GetType().Name}";

	public string Echo(string value) => value;

	public Task<string> EchoAsync(string value) => Task.FromResult("async:" + value);

	public async Task<string> SlowAsync(int delayMs)
	{
		await Task.Delay(delayMs).ConfigureAwait(false);
		return "slow-done";
	}

	/// <summary>Synchronously blocks the calling thread; used to prove a stuck invocation cannot starve
	/// other connections (a server-wide lock would deadlock everyone behind it).</summary>
	public string BlockFor(int delayMs)
	{
		Thread.Sleep(delayMs);
		return "unblocked";
	}

	public Task DoWorkAsync()
	{
		LastWorkDone = true;
		return Task.CompletedTask;
	}

	public void Reset() => LastWorkDone = false;

	public string GetStatus() => "{\"ok\":true,\"workDone\":" + (LastWorkDone ? "true" : "false") + "}";

	public bool LastWorkDone { get; private set; }
}
