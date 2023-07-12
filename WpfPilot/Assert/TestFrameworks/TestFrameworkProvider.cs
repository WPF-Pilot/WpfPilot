namespace WpfPilot.Assert.TestFrameworks;

using System;
using System.Collections.Generic;
using System.Linq;

internal static class TestFrameworkProvider
{
	public static void Throw(string message)
	{
		TestFramework ??= DetectFramework();
		TestFramework.Throw(message);
	}

	private static ITestFramework DetectFramework()
	{
		ITestFramework detectedFramework = AttemptToDetectUsingDynamicScanning() ?? new FallbackTestFramework();
		return detectedFramework;
	}

	private static ITestFramework? AttemptToDetectUsingDynamicScanning()
	{
		return Frameworks.Values.FirstOrDefault(framework => framework.IsAvailable);
	}

	private static readonly Dictionary<string, ITestFramework> Frameworks = new(StringComparer.OrdinalIgnoreCase)
	{
		["mspec"] = new MSpecFramework(),
		["nspec3"] = new NSpecFramework(),
		["nunit"] = new NUnitTestFramework(),
		["mstestv2"] = new MSTestFrameworkV2(),
		["xunit2"] = new XUnit2TestFramework() // Keep this the last one as it uses a try/catch approach
	};
	private static ITestFramework? TestFramework;
}
