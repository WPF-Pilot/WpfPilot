namespace WpfPilot.Mac.Driver;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using WpfPilot.Mac.Protocol;

/// <summary>
/// The macOS test-side entry point, analogous to the Windows WpfPilot <c>AppDriver</c>. Instead of
/// injecting a payload into a target process, it launches (or attaches to) an application that hosts an
/// in-process <c>AutomationServer</c> and connects to it over an authenticated loopback channel.
/// </summary>
public sealed class AppDriver : IDisposable
{
	private AppDriver(AutomationConnection connection, Process? process)
	{
		m_connection = connection;
		Process = process;
	}

	/// <summary>The launched application process, when this driver launched it (null when attached).</summary>
	public Process? Process { get; }

	/// <summary>
	/// Launches a macOS application (a <c>.app</c> bundle or a direct executable) that hosts an
	/// <c>AutomationServer</c>, then connects to it. The app is told where to write its discovery
	/// file via the <c>WPFPILOT_MAC_DISCOVERY</c> environment variable, and automation is enabled via
	/// <c>LOGOS_E2E_AUTOMATION=1</c> (matching the Windows harness).
	/// </summary>
	public static AppDriver Launch(string appPath, LaunchOptions? options = null)
	{
		_ = appPath ?? throw new ArgumentNullException(nameof(appPath));
		options ??= new LaunchOptions();

		var discoveryName = "logos-e2e-" + Guid.NewGuid().ToString("N");
		var discoveryPath = AutomationDiscovery.PathForName(discoveryName);
		if (File.Exists(discoveryPath))
			File.Delete(discoveryPath);

		var executable = ResolveExecutable(appPath);
		var startInfo = new ProcessStartInfo(executable)
		{
			UseShellExecute = false,
		};
		foreach (var arg in options.Arguments)
			startInfo.ArgumentList.Add(arg);

		startInfo.Environment["WPFPILOT_MAC_DISCOVERY"] = discoveryPath;
		startInfo.Environment["LOGOS_E2E_AUTOMATION"] = "1";
		foreach (var kvp in options.Environment)
			startInfo.Environment[kvp.Key] = kvp.Value;

		var launchedAtMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
		var process = Process.Start(startInfo)
			?? throw new InvalidOperationException($"Failed to launch '{executable}'.");

		var discovery = WaitForDiscovery(discoveryPath, process, launchedAtMs, options.StartupTimeout);
		var connection = ConnectAndVerify(discovery);
		return new AppDriver(connection, process);
	}

	/// <summary>Attaches to an already-running application by reading its discovery file.</summary>
	public static AppDriver Attach(string discoveryFilePath)
	{
		var discovery = AutomationDiscovery.TryRead(discoveryFilePath)
			?? throw new FileNotFoundException($"No automation discovery file at '{discoveryFilePath}'.");
		return new AppDriver(ConnectAndVerify(discovery), null);
	}

	/// <summary>Attaches directly to a known loopback port + token (used for in-process integration tests).</summary>
	public static AppDriver Attach(int port, string token) =>
		new(new AutomationConnection(port, token), null);

	/// <summary>
	/// Finds a registered automation target whose info satisfies <paramref name="predicate"/>, retrying
	/// until <paramref name="timeoutMs"/> elapses. Mirrors the Windows <c>AppDriver.GetElement</c>.
	/// </summary>
	public Element GetElement(Func<TargetInfo, bool> predicate, int timeoutMs = 30_000)
	{
		_ = predicate ?? throw new ArgumentNullException(nameof(predicate));
		var deadline = DateTime.UtcNow.AddMilliseconds(timeoutMs);
		Exception? last = null;
		while (DateTime.UtcNow < deadline)
		{
			try
			{
				var targets = GetTargets();
				var match = targets.FirstOrDefault(predicate);
				if (match is not null)
					return new Element(m_connection, match.TargetId, match.TypeName);
			}
			catch (Exception ex)
			{
				last = ex;
			}

			Thread.Sleep(250);
		}

		throw new TimeoutException($"No matching automation target found within {timeoutMs} ms." +
			(last is null ? "" : " Last error: " + last.Message));
	}

	/// <summary>Returns all currently registered automation targets.</summary>
	public IReadOnlyList<TargetInfo> GetTargets()
	{
		var response = m_connection.Send(new CommandMessage { Kind = CommandKind.GetTargets }, timeoutMs: 10_000);
		if (response.Error is not null)
			throw new InvalidOperationException("GetTargets failed: " + response.Error);
		return response.Targets ?? new List<TargetInfo>();
	}

	public void Dispose()
	{
		try
		{
			m_connection.Dispose();
		}
		catch
		{
			// ignore
		}

		try
		{
			if (Process is { HasExited: false })
				Process.Kill(entireProcessTree: true);
		}
		catch
		{
			// ignore
		}
	}

	private static AutomationConnection ConnectAndVerify(AutomationDiscovery discovery)
	{
		var connection = new AutomationConnection(discovery.Port, discovery.Token);
		var hello = connection.Send(new CommandMessage { Kind = CommandKind.Hello }, timeoutMs: 10_000);
		if (hello.Error is not null)
		{
			connection.Dispose();
			throw new InvalidOperationException("Automation handshake failed: " + hello.Error);
		}

		if (hello.InstanceId != discovery.InstanceId)
		{
			connection.Dispose();
			throw new InvalidOperationException("Connected to a different automation instance than expected (stale discovery file?).");
		}

		return connection;
	}

	private static AutomationDiscovery WaitForDiscovery(string path, Process process, long launchedAtMs, TimeSpan timeout)
	{
		var deadline = DateTime.UtcNow + timeout;
		while (DateTime.UtcNow < deadline)
		{
			if (process.HasExited)
				throw new InvalidOperationException($"Application exited (code {process.ExitCode}) before the automation server started.");

			var discovery = AutomationDiscovery.TryRead(path);

			// Guard against a stale file: require it to be at least as new as our launch and to name a live pid.
			if (discovery is not null && discovery.UnixTimeMs >= launchedAtMs && IsAlive(discovery.Pid))
				return discovery;

			Thread.Sleep(250);
		}

		throw new TimeoutException($"Automation server did not publish '{path}' within {timeout.TotalSeconds:F0}s.");
	}

	private static bool IsAlive(int pid)
	{
		try
		{
			using var _ = Process.GetProcessById(pid);
			return true;
		}
		catch
		{
			return false;
		}
	}

	private static string ResolveExecutable(string appPath)
	{
		// Direct executable.
		if (File.Exists(appPath) && !appPath.EndsWith(".app", StringComparison.OrdinalIgnoreCase))
			return appPath;

		// .app bundle: launch the inner Mach-O executable so we can pass env vars and own the child pid
		// (unlike `open`, which detaches the process).
		if (Directory.Exists(appPath) && appPath.EndsWith(".app", StringComparison.OrdinalIgnoreCase))
		{
			var macOsDir = Path.Combine(appPath, "Contents", "MacOS");
			if (!Directory.Exists(macOsDir))
				throw new FileNotFoundException($"'{appPath}' is not a valid .app bundle (no Contents/MacOS).");

			var bundleName = Path.GetFileNameWithoutExtension(appPath);
			var preferred = Path.Combine(macOsDir, bundleName);
			if (File.Exists(preferred))
				return preferred;

			var firstExecutable = Directory.GetFiles(macOsDir)
				.FirstOrDefault(f => !f.EndsWith(".app", StringComparison.OrdinalIgnoreCase));
			if (firstExecutable is not null)
				return firstExecutable;
		}

		throw new FileNotFoundException($"Could not resolve an executable from '{appPath}'.");
	}

	private readonly AutomationConnection m_connection;
}

/// <summary>Options controlling how <see cref="AppDriver.Launch"/> starts the application.</summary>
public sealed class LaunchOptions
{
	/// <summary>Extra command-line arguments passed to the application executable.</summary>
	public IList<string> Arguments { get; } = new List<string>();

	/// <summary>Extra environment variables set on the launched process (e.g. E2E auth tokens).</summary>
	public IDictionary<string, string> Environment { get; } = new Dictionary<string, string>();

	/// <summary>How long to wait for the application to publish its automation discovery file.</summary>
	public TimeSpan StartupTimeout { get; set; } = TimeSpan.FromMinutes(5);
}
