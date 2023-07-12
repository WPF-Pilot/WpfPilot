namespace WpfPilot.Utility;

using System;
using System.IO;
using System.Linq;
using Newtonsoft.Json;

internal static class Log
{
	public static void Info(string message, params object[] args)
	{
		EnsureLogExists();
		var serializedArgs = args.Length != 0 ?
			string.Join("\n", args.Select(CreateLogString)) + "\n" :
			"";
		try
		{
			File.AppendAllText(LogFilePath, $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} [INFO] {message}\n{serializedArgs}");
		}
		catch
		{
			// Can happen if two separate threads/processes try to write to the log at the same time.
			// While not great, it's not worth crashing the app over.
		}
	}

	public static void Error(string message, params object[] args)
	{
		EnsureLogExists();
		var serializedArgs = args.Length != 0 ?
			string.Join("\n", args.Select(CreateLogString)) + "\n" :
			"";
		try
		{
			File.AppendAllText(LogFilePath, $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} [ERROR] {message}\n{serializedArgs}");
		}
		catch
		{
			// Can happen if two separate threads/processes try to write to the log at the same time.
			// While not great, it's not worth crashing the app over.
		}
	}

	public static string CreateLogString(object item)
	{
		try
		{
			return JsonConvert.SerializeObject(item);
		}
		catch
		{
			return item.ToString() ?? "";
		}
	}

	private static void EnsureLogExists()
	{
		if (File.Exists(LogFilePath))
			return;

		Directory.CreateDirectory(Path.GetDirectoryName(LogFilePath)!);
		try
		{
			File.WriteAllText(LogFilePath, string.Empty);
		}
		catch
		{
			// Can happen if two separate threads/processes try to write to the log at the same time.
			// While not great, it's not worth crashing the app over.
		}
	}

	internal static readonly string LogFilePath = Path.Combine(Temp.Location, $"log-{Guid.NewGuid()}.txt");
}