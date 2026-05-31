namespace WpfPilot.Mac.Server;

using System;

/// <summary>
/// Abstraction for marshaling automation invocations onto the application's main/UI thread.
/// </summary>
/// <remarks>
/// On Windows, WpfPilot ran injected commands on the WPF dispatcher thread. The macOS host
/// (Logos.app) similarly requires that app-state and UI access happen on its main thread. The
/// application supplies an implementation when it starts the <see cref="AutomationServer"/>; tests and
/// background-safe hosts can use <see cref="PassthroughMainThreadInvoker"/>.
/// </remarks>
public interface IMainThreadInvoker
{
	/// <summary>Runs <paramref name="action"/> on the main thread and returns its result.</summary>
	T Invoke<T>(Func<T> action);
}

/// <summary>An invoker that runs work synchronously on the calling thread (no marshaling).</summary>
public sealed class PassthroughMainThreadInvoker : IMainThreadInvoker
{
	public T Invoke<T>(Func<T> action) => action();
}
