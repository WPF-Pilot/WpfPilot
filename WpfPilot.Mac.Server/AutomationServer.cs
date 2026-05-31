namespace WpfPilot.Mac.Server;

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using WpfPilot.Mac.Protocol;
using WpfPilot.Mac.Transport;

/// <summary>
/// In-process automation endpoint hosted by the application under test. This is the macOS replacement
/// for WpfPilot's Windows DLL injection: rather than a test process injecting a payload, the app starts
/// this server itself (gated behind an env var) and registers the objects it wants to expose for remote
/// invocation. The matching test-side <see cref="WpfPilot.Mac.Driver.AppDriver"/> connects over loopback
/// TCP and drives them.
/// </summary>
/// <remarks>
/// Security: the listener binds to loopback only and requires a per-launch token (written, owner-only,
/// to the discovery file) on every frame. Frames are rejected before the body is acted upon if the token
/// does not match. Do not start the server outside controlled E2E runs.
/// </remarks>
public sealed class AutomationServer : IDisposable
{
	public AutomationServer(IMainThreadInvoker? mainThreadInvoker = null)
	{
		m_dispatcher = new InvocationDispatcher(mainThreadInvoker ?? new PassthroughMainThreadInvoker());
		InstanceId = Guid.NewGuid().ToString("N");
		Token = Guid.NewGuid().ToString("N");
	}

	public string InstanceId { get; }
	public string Token { get; }
	public int Port { get; private set; }

	/// <summary>Registers an object for remote invocation and returns its target id.</summary>
	public string Register(object target, string? typeName = null)
	{
		_ = target ?? throw new ArgumentNullException(nameof(target));
		var id = Guid.NewGuid().ToString("N");
		m_targets[id] = new Registration(target, typeName ?? target.GetType().Name);
		return id;
	}

	/// <summary>
	/// Starts listening on a loopback port and writes a discovery file (owner-only) describing the port
	/// and token so a driver can connect. Returns the chosen port.
	/// </summary>
	public int Start(string? discoveryFilePath = null)
	{
		m_listener = new TcpListener(IPAddress.Loopback, 0);
		m_listener.Start();
		Port = ((IPEndPoint) m_listener.LocalEndpoint).Port;

		m_discoveryPath = discoveryFilePath;
		if (m_discoveryPath is not null)
		{
			new AutomationDiscovery
			{
				Port = Port,
				Token = Token,
				Pid = Process.GetCurrentProcess().Id,
				InstanceId = InstanceId,
				UnixTimeMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
			}.Write(m_discoveryPath);
		}

		m_acceptThread = new Thread(AcceptLoop) { IsBackground = true, Name = "WpfPilotMac-Accept" };
		m_acceptThread.Start();
		return Port;
	}

	public void Dispose()
	{
		m_disposed = true;
		try
		{
			m_listener?.Stop();
		}
		catch
		{
			// ignore
		}

		try
		{
			if (m_discoveryPath is not null && File.Exists(m_discoveryPath))
				File.Delete(m_discoveryPath);
		}
		catch
		{
			// ignore
		}
	}

	private void AcceptLoop()
	{
		while (!m_disposed)
		{
			TcpClient client;
			try
			{
				client = m_listener!.AcceptTcpClient();
			}
			catch
			{
				return;
			}

			var thread = new Thread(() => HandleClient(client)) { IsBackground = true, Name = "WpfPilotMac-Client" };
			thread.Start();
		}
	}

	private void HandleClient(TcpClient client)
	{
		using (client)
		using (var stream = client.GetStream())
		{
			while (!m_disposed)
			{
				FrameProtocol.RawFrame raw;
				try
				{
					raw = FrameProtocol.ReadRawFrame(stream);
				}
				catch
				{
					return;
				}

				if (raw.EndOfStream)
					return;

				// Authenticate on the plaintext token prefix BEFORE running the polymorphic deserializer
				// (TypeNameHandling.All) on attacker-controlled bytes. An unauthenticated frame is rejected
				// without ever being deserialized.
				if (raw.Token is null || !ConstantTimeEquals(raw.Token, Token))
				{
					Respond(stream, new ResponseMessage { Error = "unauthorized" });
					return;
				}

				CommandMessage? command;
				try
				{
					command = FrameProtocol.Unpack<CommandMessage>(raw.Payload!);
				}
				catch
				{
					return;
				}

				if (command is null)
					return;

				Respond(stream, ProcessCommand(command));
			}
		}
	}

	private ResponseMessage ProcessCommand(CommandMessage command)
	{
		try
		{
			switch (command.Kind)
			{
				case CommandKind.Hello:
					return new ResponseMessage { InstanceId = InstanceId, Pid = Process.GetCurrentProcess().Id };

				case CommandKind.GetTargets:
					return new ResponseMessage
					{
						Targets = m_targets.Select(kvp => new TargetInfo(kvp.Key, kvp.Value.TypeName)).ToList(),
					};

				case CommandKind.InvokeMethod:
					return ProcessInvoke(command);

				default:
					return new ResponseMessage { Error = $"unknown-command:{command.Kind}" };
			}
		}
		catch (Exception ex)
		{
			return new ResponseMessage { Error = ex.ToString() };
		}
	}

	private ResponseMessage ProcessInvoke(CommandMessage command)
	{
		if (command.TargetId is null || !m_targets.TryGetValue(command.TargetId, out var registration))
			return new ResponseMessage { Error = "StaleElement" };
		if (string.IsNullOrEmpty(command.Method))
			return new ResponseMessage { Error = "missing-method" };

		var args = (command.Args ?? new List<object?>())
			.Select(ValueMarshal.Unwrap)
			.ToList();

		var timeoutMs = command.TimeoutMs <= 0 ? Timeout.Infinite : command.TimeoutMs;

		// Run the entire dispatch (including its synchronous main-thread hop) on a worker task so the
		// timeout below actually bounds it. An async method runs synchronously up to its first await, so
		// the main-thread Invoke would otherwise execute before InvokeAsync returns a Task and would not
		// be covered by the wait. We deliberately do NOT hold a server-wide lock here: a stuck invocation
		// must not be able to starve other connections. Per-target serialization is provided naturally by
		// the single-threaded main-thread invoker.
		var invocation = Task.Run(() => m_dispatcher.InvokeAsync(registration.Target, command.Method!, args, command.IsAsync));

		try
		{
			if (!invocation.Wait(timeoutMs))
				return new ResponseMessage { Error = "timeout" };
		}
		catch (AggregateException ex)
		{
			return new ResponseMessage { Error = (ex.InnerException ?? ex).ToString() };
		}

		return new ResponseMessage { Value = ValueMarshal.Wrap(invocation.Result) };
	}

	private static void Respond(Stream stream, ResponseMessage response)
	{
		try
		{
			FrameProtocol.WriteFrame(stream, response);
		}
		catch
		{
			// Client likely disconnected; nothing actionable.
		}
	}

	private static bool ConstantTimeEquals(string a, string b)
	{
		if (a.Length != b.Length)
			return false;
		var diff = 0;
		for (var i = 0; i < a.Length; i++)
			diff |= a[i] ^ b[i];
		return diff == 0;
	}

	private readonly InvocationDispatcher m_dispatcher;
	private readonly ConcurrentDictionary<string, Registration> m_targets = new();
	private TcpListener? m_listener;
	private Thread? m_acceptThread;
	private string? m_discoveryPath;
	private volatile bool m_disposed;

	private readonly struct Registration
	{
		public Registration(object target, string typeName)
		{
			Target = target;
			TypeName = typeName;
		}

		public object Target { get; }
		public string TypeName { get; }
	}
}
