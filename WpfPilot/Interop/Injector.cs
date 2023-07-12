namespace WpfPilot.Interop;

using System;
using System.Diagnostics;
using System.IO;
using WpfPilot.Utility.WindowsAPI;

internal static class Injector
{
	public static void InjectAppDriver(WpfProcess process, string pipeName, string dllPath)
	{
		var injectorExe = process.Architecture switch
		{
			"x64" => Path.Combine(dllPath, @"WpfPilotResources\x64\WpfPilot.Injector.exe"),
			"x86" => Path.Combine(dllPath, @"WpfPilotResources\x86\WpfPilot.Injector.exe"),
			_ => throw new NotImplementedException($"Process with architecture `{process.Architecture}` is unsupported."),
		};

		var isElevated = NativeMethods.IsProcessElevated(process.Process);
		var processStartInfo = new ProcessStartInfo(injectorExe)
		{
			Arguments = $"{pipeName}?{dllPath}?{process.Id}",
			UseShellExecute = false,
			CreateNoWindow = true,
			WindowStyle = ProcessWindowStyle.Hidden,
			RedirectStandardError = true,
			Verb = isElevated ? "runas" : "",
		};

		using var injector = Process.Start(processStartInfo);
		injector?.WaitForExit();
		if (injector?.ExitCode != 0)
			throw new InvalidOperationException(injector?.StandardError.ReadToEnd());
	}
}