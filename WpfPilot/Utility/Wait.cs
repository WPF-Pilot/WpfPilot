namespace WpfPilot.Utility;

using System;
using System.Threading.Tasks;

internal static class Wait
{
	public static bool Until(Func<bool> condition, int timeoutMs, int retryIntervalMs = 10)
	{
		var start = DateTime.Now;
		while (!condition())
		{
			if ((DateTime.Now - start).TotalMilliseconds > timeoutMs)
				return false;

			Task.Delay(retryIntervalMs).GetAwaiter().GetResult();
		}

		return true;
	}
}
