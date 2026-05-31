namespace WpfPilot.Mac.Protocol;

using System;
using System.Collections.Generic;

/// <summary>
/// Command kinds exchanged between the macOS <c>AppDriver</c> (test process) and the in-process
/// <c>AutomationServer</c> hosted by the application under test.
/// </summary>
public static class CommandKind
{
	public const string Hello = "Hello";
	public const string GetTargets = "GetTargets";
	public const string InvokeMethod = "InvokeMethod";
}

/// <summary>
/// A single request frame. Mirrors the Windows WpfPilot command envelope, but drives a method-name RPC
/// instead of marshaling a serialized expression tree (which is fragile across process/assembly
/// boundaries). The per-launch auth token is carried by the transport envelope, not this body.
/// </summary>
public sealed class CommandMessage
{
	public string Kind { get; set; } = "";

	/// <summary>Target object id for <see cref="CommandKind.InvokeMethod"/>.</summary>
	public string? TargetId { get; set; }

	/// <summary>Method name to invoke on the target.</summary>
	public string? Method { get; set; }

	/// <summary>
	/// Method arguments, each wrapped (via <c>WrappedArg</c>) so primitive types such as <see cref="int"/>
	/// survive the JSON round-trip instead of widening to <see cref="long"/>.
	/// </summary>
	public List<object?>? Args { get; set; }

	/// <summary>True when the invoked method returns a <see cref="System.Threading.Tasks.Task"/> to await.</summary>
	public bool IsAsync { get; set; }

	public int TimeoutMs { get; set; } = 30_000;
}

/// <summary>A single response frame.</summary>
public sealed class ResponseMessage
{
	/// <summary>The (wrapped) return value on success.</summary>
	public object? Value { get; set; }

	/// <summary>Non-null when the command failed; carries the server-side error text.</summary>
	public string? Error { get; set; }

	/// <summary>Populated for <see cref="CommandKind.GetTargets"/>.</summary>
	public List<TargetInfo>? Targets { get; set; }

	/// <summary>Populated for <see cref="CommandKind.Hello"/>; identifies the responding server instance.</summary>
	public string? InstanceId { get; set; }

	public int Pid { get; set; }
}

/// <summary>Describes a registered automation target (object exposed for remote invocation).</summary>
public sealed class TargetInfo
{
	public TargetInfo()
	{
	}

	public TargetInfo(string targetId, string typeName)
	{
		TargetId = targetId;
		TypeName = typeName;
	}

	public string TargetId { get; set; } = "";
	public string TypeName { get; set; } = "";
}
