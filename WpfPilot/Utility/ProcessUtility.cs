namespace WpfPilot.Utility;

using System.IO;
using static WpfPilot.Utility.Retry;

internal static class ProcessUtility
{
	public static void ThrowOnAppErrorCode(int? processExitCode, string pipeName)
	{
		// App is still running or exited gracefully.
		if (processExitCode == null || processExitCode == 0)
			return;

		var exceptionMessage = "App has unexpectedly exited.";

		var crashLogExists = Wait.Until(() => File.Exists($"{pipeName}-crash.txt"), timeoutMs: 500, retryIntervalMs: 100);
		if (crashLogExists)
		{
			var lastUnhandledException = ExceptionLog.ReadLog($"{pipeName}-crash.txt");
			if (lastUnhandledException.Length != 0)
				exceptionMessage += $" Last unhandled exception:\n{lastUnhandledException}";
		}

		throw new RetryException(exceptionMessage);
	}
}
