// https://github.com/dotnet/runtime/issues/78270#issuecomment-1330402188
#pragma warning disable CA1852

using System;
using Faithlife.Build;

return BuildRunner.Execute(args, build =>
{
	build.AddDotNetTargets(
		new DotNetBuildSettings
		{
			// Build/pack the Windows WpfPilot package plus the three cross-platform macOS automation
			// packages (WpfPilot.Mac, .Server, .Driver). The native projects
			// (WpfPilot.InjectionDll.vcxproj, WpfPilot.Injector) cannot be built with the dotnet CLI
			// and their prebuilt binaries are committed under WpfPilot/contentFiles, so they are kept
			// out of this publish solution.
			SolutionName = "WpfPilot.Publish.sln",
			NuGetApiKey = Environment.GetEnvironmentVariable("NUGET_API_KEY"),
			PackageSettings = new DotNetPackageSettings
			{
				PushTagOnPublish = x => $"v{x.Version}",
			},
		});
});