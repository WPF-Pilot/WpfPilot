namespace WpfPilot.Interop;

using System;
using System.Diagnostics;
using WpfPilot.Utility.WindowsAPI;

internal sealed class WpfProcess
{
	public WpfProcess(Process process)
	{
		Process = process ?? throw new ArgumentNullException(nameof(process));
		Id = process.Id;
		Handle = NativeMethods.OpenProcess(NativeMethods.ProcessAccessFlags.All, false, process.Id);
		Architecture = NativeMethods.GetArchitectureWithoutException(Process);
		SupportedFrameworkName = GetSupportedTargetFramework(process);

		if (Architecture.Contains("ARM"))
			throw new InvalidOperationException("ARM processes are not supported.");
	}

	public Process Process { get; }

	public int Id { get; }

	public NativeMethods.ProcessHandle Handle { get; private set; }

	public string Architecture { get; private set; }

	public string SupportedFrameworkName { get; private set; }

	public void Refresh()
	{
		Process.Refresh();
		Handle = NativeMethods.OpenProcess(NativeMethods.ProcessAccessFlags.All, false, Process.Id);
		Architecture = NativeMethods.GetArchitectureWithoutException(Process);
		SupportedFrameworkName = GetSupportedTargetFramework(Process);
	}

	public static string GetSupportedTargetFramework(Process process)
	{
		var modules = NativeMethods.GetModules(process);

		FileVersionInfo? systemRuntimeVersion = null;
		FileVersionInfo? wpfGFXVersion = null;

		foreach (var module in modules)
		{
			if (module.szModule.StartsWith("wpfgfx_", StringComparison.OrdinalIgnoreCase))
				wpfGFXVersion = FileVersionInfo.GetVersionInfo(module.szExePath);
			else if (module.szModule.StartsWith("System.Runtime.dll", StringComparison.OrdinalIgnoreCase))
				systemRuntimeVersion = FileVersionInfo.GetVersionInfo(module.szExePath);
		}

		var relevantVersionInfo = systemRuntimeVersion ?? wpfGFXVersion;
		if (relevantVersionInfo is null)
			return "netframework";

		// Modified from the standard snoop injector. The injector DLLs were also modified to match.
		var productVersion = TryParseVersion(relevantVersionInfo.ProductVersion ?? string.Empty);
		return relevantVersionInfo.ProductMajorPart switch
		{
			>= 5 => "dotnet",
			4 => "netframework",
			3 when productVersion.Minor >= 1 => "netcoreapp",
			_ => throw new NotSupportedException($".NET version `{relevantVersionInfo.ProductVersion}` is not supported.")
		};
	}

	private static Version TryParseVersion(string version)
	{
		var versionToParse = version;

		var previewVersionMarkerIndex = versionToParse.IndexOfAny(new[] { '-', '+' });

		if (previewVersionMarkerIndex > -1)
			versionToParse = version.Substring(0, previewVersionMarkerIndex);

		if (Version.TryParse(versionToParse, out var parsedVersion))
			return parsedVersion;

		return new Version();
	}
}