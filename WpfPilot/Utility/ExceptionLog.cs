namespace WpfPilot.Utility;

using System;
using System.IO;

internal static class ExceptionLog
{
	public static UnhandledExceptionEventHandler Handler(string logName)
	{
		return (object sender, UnhandledExceptionEventArgs e) =>
		{
			var path = Path.Combine(Temp.Location,  logName);
			File.WriteAllText(path, ((Exception) e.ExceptionObject).ToString());
		};
	}

	public static string ReadLog(string logName)
	{
		var path = Path.Combine(Temp.Location, logName);
		return File.Exists(path) ? File.ReadAllText(path) : "";
	}
}
