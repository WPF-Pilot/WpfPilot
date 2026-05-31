namespace WpfPilot.Mac.SampleHost;

using System;
using System.Threading;
using WpfPilot.Mac.Server;

/// <summary>
/// Minimal application that hosts an <see cref="AutomationServer"/> exactly the way the real Logos.app
/// would when <c>LOGOS_E2E_AUTOMATION=1</c>: it registers its host object and publishes a discovery file
/// at the path given by <c>WPFPILOT_MAC_DISCOVERY</c>, then idles so a driver can connect and invoke.
/// </summary>
internal static class Program
{
	public static int Main(string[] args)
	{
		var discoveryPath = Environment.GetEnvironmentVariable("WPFPILOT_MAC_DISCOVERY");
		if (string.IsNullOrEmpty(discoveryPath))
		{
			Console.Error.WriteLine("WPFPILOT_MAC_DISCOVERY is not set.");
			return 2;
		}

		using var server = new AutomationServer();
		server.Register(new SampleAutomationHost(), typeName: "E2EAutomationHost");
		var port = server.Start(discoveryPath);
		Console.WriteLine($"SampleHost automation server listening on 127.0.0.1:{port}");

		// Idle until killed by the driver (or a safety timeout so stray processes don't linger forever).
		Thread.Sleep(TimeSpan.FromMinutes(5));
		return 0;
	}
}
