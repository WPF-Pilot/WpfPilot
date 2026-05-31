namespace WpfPilot.Mac.Protocol;

using System;
using System.IO;
using Newtonsoft.Json;

/// <summary>
/// Discovery handshake written by the in-process <c>AutomationServer</c> and read by the
/// <c>AppDriver</c> so the driver can find the loopback port + auth token of a launched app.
/// </summary>
/// <remarks>
/// The default location is <c>$TMPDIR/wpfpilot-mac/&lt;name&gt;.json</c>. The file is written with
/// owner-only permissions because it carries the automation token. It includes the pid and a per-launch
/// instance id so a driver can detect (and ignore) a stale file left by a previous or unrelated run.
/// </remarks>
public sealed class AutomationDiscovery
{
	public int Port { get; set; }
	public string Token { get; set; } = "";
	public int Pid { get; set; }
	public string InstanceId { get; set; } = "";
	public long UnixTimeMs { get; set; }

	/// <summary>The default discovery directory, honoring <c>TMPDIR</c>.</summary>
	public static string DefaultDirectory =>
		Path.Combine(Path.GetTempPath(), "wpfpilot-mac");

	/// <summary>Resolves the discovery file path for a given logical automation name.</summary>
	public static string PathForName(string name) =>
		Path.Combine(DefaultDirectory, name + ".json");

	public void Write(string path)
	{
		var dir = Path.GetDirectoryName(path)!;
		Directory.CreateDirectory(dir);
		var json = JsonConvert.SerializeObject(this);

		// Write then tighten permissions to owner read/write only (best effort; the token guards access).
		File.WriteAllText(path, json);
		TrySetOwnerOnlyPermissions(path);
	}

	public static AutomationDiscovery? TryRead(string path)
	{
		try
		{
			if (!File.Exists(path))
				return null;
			return JsonConvert.DeserializeObject<AutomationDiscovery>(File.ReadAllText(path));
		}
		catch
		{
			return null;
		}
	}

	private static void TrySetOwnerOnlyPermissions(string path)
	{
		try
		{
			// 0600
			File.SetUnixFileMode(path, UnixFileMode.UserRead | UnixFileMode.UserWrite);
		}
		catch
		{
			// Not supported on every platform/filesystem; the token remains the real access guard.
		}
	}
}
