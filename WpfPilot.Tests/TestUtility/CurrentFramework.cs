﻿namespace WpfPilot.Tests.TestUtility;

using System;
using NUnit.Framework;

public static class CurrentFramework
{
	public static string GetVersion()
	{
		var cwd = TestContext.CurrentContext.WorkDirectory;
		return cwd switch
		{
			_ when cwd.Contains("net9.0") => "net9.0-windows",
			_ when cwd.Contains("net8.0") => "net8.0-windows",
			_ when cwd.Contains("net7.0") => "net7.0-windows",
			_ when cwd.Contains("net6.0") => "net6.0-windows",
			_ when cwd.Contains("net5.0") => "net5.0-windows",
			_ when cwd.Contains("net452") => "net452",
			_ when cwd.Contains("netcoreapp") => "netcoreapp3.1",
			_ => throw new NotSupportedException($"WPF Pilot {nameof(CurrentFramework)}.{nameof(GetVersion)} does not support this version."),
		};
	}
}
