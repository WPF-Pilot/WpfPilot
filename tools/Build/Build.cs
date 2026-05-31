// https://github.com/dotnet/runtime/issues/78270#issuecomment-1330402188
#pragma warning disable CA1852

using System;
using Faithlife.Build;

return BuildRunner.Execute(args, build =>
{
	build.AddDotNetTargets(
		new DotNetBuildSettings
		{
			// Only the WpfPilot package project is built/packed here. The native projects
			// (WpfPilot.InjectionDll.vcxproj, WpfPilot.Injector) cannot be built with the
			// dotnet CLI and their prebuilt binaries are committed under WpfPilot/contentFiles.
			SolutionName = "WpfPilot/WpfPilot.csproj",
			NuGetApiKey = Environment.GetEnvironmentVariable("NUGET_API_KEY"),
			PackageSettings = new DotNetPackageSettings
			{
				PushTagOnPublish = x => $"v{x.Version}",
			},
		});
});