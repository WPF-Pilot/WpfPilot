namespace WpfPilot.Mac.Tests;

using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using WpfPilot.Mac.Driver;
using WpfPilot.Mac.Protocol;
using WpfPilot.Mac.SampleHost;

/// <summary>
/// End-to-end tests that launch the <c>WpfPilot.Mac.SampleHost</c> as a separate process (the way the
/// driver will launch Logos.app), wait for it to publish its discovery file, connect over loopback, and
/// invoke methods. This proves the full launch → discover → authenticate → invoke pipeline on macOS,
/// across a real process boundary, with the target type living in a different assembly from the driver.
/// </summary>
[TestFixture]
public sealed class LaunchEndToEndTests
{
	[Test]
	public void Launch_DiscoversAndInvokesAcrossProcessBoundary()
	{
		var executable = ResolveSampleHostExecutable();
		if (executable is null)
			Assert.Ignore("SampleHost executable not found; build WpfPilot.Mac.SampleHost first.");

		using var driver = AppDriver.Launch(executable!, new LaunchOptions
		{
			StartupTimeout = TimeSpan.FromSeconds(60),
		});

		var element = driver.GetElement(x => x.TypeName == "E2EAutomationHost", timeoutMs: 15_000);
		Assert.Multiple(() =>
		{
			Assert.That(element.Invoke<SampleAutomationHost, string>(x => x.Ping()), Is.EqualTo("pong"));
			Assert.That(element.Invoke<SampleAutomationHost, int>(x => x.Add(20, 22)), Is.EqualTo(42));
			Assert.That(element.InvokeAsync<SampleAutomationHost, string>(x => x.EchoAsync("x")), Is.EqualTo("async:x"));
		});
	}

	[Test]
	public void DiscoveryFile_RoundTrips()
	{
		var path = Path.Combine(Path.GetTempPath(), "wpfpilot-mac-test-" + Guid.NewGuid().ToString("N") + ".json");
		try
		{
			var original = new AutomationDiscovery
			{
				Port = 54321,
				Token = "tok",
				Pid = 4242,
				InstanceId = "abc",
				UnixTimeMs = 1234567890,
			};
			original.Write(path);

			var read = AutomationDiscovery.TryRead(path);
			Assert.That(read, Is.Not.Null);
			Assert.That(read!.Port, Is.EqualTo(54321));
			Assert.That(read.Token, Is.EqualTo("tok"));
			Assert.That(read.InstanceId, Is.EqualTo("abc"));
		}
		finally
		{
			if (File.Exists(path))
				File.Delete(path);
		}
	}

	private static string? ResolveSampleHostExecutable()
	{
		const string appHostName = "WpfPilot.Mac.SampleHost";

		// Common case: the apphost is built alongside the test assembly (shared output directory).
		var sibling = Path.Combine(AppContext.BaseDirectory, appHostName);
		if (File.Exists(sibling) && !IsManagedDll(sibling))
			return sibling;

		// Otherwise walk up to the repo root and locate the SampleHost apphost under its own bin.
		var dir = new DirectoryInfo(AppContext.BaseDirectory);
		while (dir is not null && !Directory.Exists(Path.Combine(dir.FullName, appHostName)))
			dir = dir.Parent;

		if (dir is null)
			return null;

		var sampleHostBin = Path.Combine(dir.FullName, appHostName, "bin");
		if (!Directory.Exists(sampleHostBin))
			return null;

		var tfm = new DirectoryInfo(AppContext.BaseDirectory).Name;
		var candidates = Directory.GetFiles(sampleHostBin, appHostName, SearchOption.AllDirectories)
			.Where(f => !IsManagedDll(f))
			.OrderByDescending(f => f.Contains(Path.DirectorySeparatorChar + tfm + Path.DirectorySeparatorChar))
			.ToList();

		return candidates.FirstOrDefault(File.Exists);
	}

	private static bool IsManagedDll(string path) =>
		path.EndsWith(".dll", StringComparison.OrdinalIgnoreCase);
}
