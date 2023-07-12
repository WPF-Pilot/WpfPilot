namespace WpfPilot.Utility;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

internal static class Retry
{
	public static void With(Action action, int retryIntervalMs, int retryCount)
	{
		var exceptions = new List<Exception>();
		for (var i = 0; i < retryCount; i++)
		{
			try
			{
				action();
				return;
			}
			catch (Exception e)
			{
				if (e is RetryException)
					throw e;

				exceptions.Add(e);
				Task.Delay(retryIntervalMs).GetAwaiter().GetResult();
			}
		}

		throw new AggregateException(exceptions);
	}

	public sealed class RetryException : Exception
	{
		public RetryException(string message)
			: base(message)
		{
		}
	}
}
