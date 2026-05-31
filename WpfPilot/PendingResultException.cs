namespace WpfPilot;

using System;

/// <summary>
/// Thrown internally when a visual-tree query cannot complete because the app's WPF UI thread is
/// momentarily blocked by a modal dialog (the payload reports <c>PendingResult</c>). This is a
/// transient condition during app startup (boot/sign-in screens) and while modal dialogs are open,
/// so element-lookup loops treat it as retryable rather than a hard failure.
/// </summary>
public sealed class PendingResultException : TimeoutException
{
	public PendingResultException(string message)
		: base(message)
	{
	}
}
