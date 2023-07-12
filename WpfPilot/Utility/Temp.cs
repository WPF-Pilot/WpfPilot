namespace WpfPilot.Utility;

using System;
using System.IO;

internal static class Temp
{
	public static void CleanStaleFiles()
	{
		Directory.CreateDirectory(Location);

		// Delete files older than a few days.
		var files = Directory.GetFiles(Location);
		foreach (var file in files)
		{
			var fileInfo = new FileInfo(file);
			if (fileInfo.LastWriteTimeUtc < DateTime.UtcNow.AddDays(-2) && file.EndsWith(".txt"))
			{
				try
				{
					File.Delete(file);
				}
				catch
				{
					// Ignore permissions errors, file in use errors, etc.
				}
			}
		}
	}

	public static readonly string Location = Path.Combine(Path.GetTempPath(), "WpfPilot");
}
