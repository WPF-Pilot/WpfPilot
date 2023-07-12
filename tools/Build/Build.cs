// https://github.com/dotnet/runtime/issues/78270#issuecomment-1330402188
#pragma warning disable CA1852

using System;
using Faithlife.Build;

return BuildRunner.Execute(args, build =>
{
	build.AddDotNetTargets(
		new DotNetBuildSettings
		{
			SolutionPlatform = "x86", // Injector will auto build for x64 after x86 is done.
			NuGetApiKey = Environment.GetEnvironmentVariable("NUGET_API_KEY"),
			PackageSettings = new DotNetPackageSettings
			{
				PushTagOnPublish = x => $"v{x.Version}",
			},
		});
});