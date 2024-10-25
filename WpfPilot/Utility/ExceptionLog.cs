namespace WpfPilot.Utility;

using System;
using System.IO;
using System.Windows.Threading;

internal static class ExceptionLog
{
	public static DispatcherUnhandledExceptionEventHandler DispatchHandler(string logName)
	{
		return (object sender, DispatcherUnhandledExceptionEventArgs e) =>
		{
			var path = Path.Combine(Temp.Location, logName);
			File.WriteAllText(path, e.Exception.ToString());
		};
	}

	public static UnhandledExceptionEventHandler Handler(string logName)
	{
		return (object sender, UnhandledExceptionEventArgs e) =>
		{
			var path = Path.Combine(Temp.Location, logName);
			File.WriteAllText(path, ((Exception) e.ExceptionObject).ToString());
		};
	}

	public static string ReadLog(string logName)
	{
		var path = Path.Combine(Temp.Location, logName);
		return File.Exists(path) ? File.ReadAllText(path) : "";
	}
}
