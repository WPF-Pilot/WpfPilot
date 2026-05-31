namespace WpfPilot.Mac.Tests;

using System;
using System.Linq;
using NUnit.Framework;
using WpfPilot.Mac.Driver;
using WpfPilot.Mac.Server;
using WpfPilot.Mac.SampleHost;

/// <summary>
/// In-process tests for the macOS automation transport: a driver and an <see cref="AutomationServer"/>
/// running in the same process, connected over the real loopback TCP channel. These exercise the exact
/// path the Logos macOS E2E host will use (method-name RPC, primitive fidelity, async, auth, errors).
/// </summary>
[TestFixture]
public sealed class AutomationServerTests
{
	[SetUp]
	public void SetUp()
	{
		m_host = new SampleAutomationHost();
		m_server = new AutomationServer();
		m_targetId = m_server.Register(m_host, typeName: "E2EAutomationHost");
		var port = m_server.Start();
		m_driver = AppDriver.Attach(port, m_server.Token);
	}

	[TearDown]
	public void TearDown()
	{
		m_driver?.Dispose();
		m_server?.Dispose();
	}

	[Test]
	public void GetElement_FindsRegisteredHostByTypeName()
	{
		var element = m_driver!.GetElement(x => x.TypeName == "E2EAutomationHost", timeoutMs: 5_000);
		Assert.That(element.TypeName, Is.EqualTo("E2EAutomationHost"));
		Assert.That(element.TargetId, Is.EqualTo(m_targetId));
	}

	[Test]
	public void Invoke_SyncMethod_ReturnsResult()
	{
		var element = Host();
		Assert.That(element.Invoke<SampleAutomationHost, string>(x => x.Ping()), Is.EqualTo("pong"));
	}

	[Test]
	public void Invoke_PreservesIntArguments()
	{
		var element = Host();

		// Result must come back as an int (2 + 3), not a widened long.
		var sum = element.Invoke<SampleAutomationHost, int>(x => x.Add(2, 3));
		Assert.That(sum, Is.EqualTo(5));

		// And the argument must arrive on the server as Int32, not Int64.
		Assert.That(element.Invoke<SampleAutomationHost, string>(x => x.Describe(7)), Is.EqualTo("int:7:Int32"));
	}

	[Test]
	public void Invoke_PassesStringArguments()
	{
		var element = Host();
		Assert.That(element.Invoke<SampleAutomationHost, string>(x => x.Echo("hello")), Is.EqualTo("hello"));
	}

	[Test]
	public void Invoke_WithCapturedLocalArgument()
	{
		var element = Host();
		var captured = 41;
		Assert.That(element.Invoke<SampleAutomationHost, int>(x => x.Add(captured, 1)), Is.EqualTo(42));
	}

	[Test]
	public void InvokeAsync_ReturnsAwaitedResult()
	{
		var element = Host();
		Assert.That(element.InvokeAsync<SampleAutomationHost, string>(x => x.EchoAsync("hi")), Is.EqualTo("async:hi"));
	}

	[Test]
	public void InvokeAsync_VoidTask_RunsOnServer()
	{
		var element = Host();
		element.Invoke<SampleAutomationHost>(x => x.Reset());
		element.InvokeAsync<SampleAutomationHost>(x => x.DoWorkAsync());
		Assert.That(element.Invoke<SampleAutomationHost, string>(x => x.GetStatus()), Does.Contain("\"workDone\":true"));
	}

	[Test]
	public void Invoke_VoidMethod_ChainsAndExecutes()
	{
		var element = Host();
		element.InvokeAsync<SampleAutomationHost>(x => x.DoWorkAsync());
		element.Invoke<SampleAutomationHost>(x => x.Reset());
		Assert.That(element.Invoke<SampleAutomationHost, string>(x => x.GetStatus()), Does.Contain("\"workDone\":false"));
	}

	[Test]
	public void Invoke_RespectsServerSideTimeout()
	{
		var element = Host();
		var ex = Assert.Throws<InvalidOperationException>(() =>
			element.InvokeAsync<SampleAutomationHost, string>(x => x.SlowAsync(3_000), timeoutMs: 250));
		Assert.That(ex!.Message, Does.Contain("timeout"));
	}

	[Test]
	public void Invoke_StuckInvocation_DoesNotStarveOtherConnections()
	{
		// A synchronously-blocked invocation must time out without holding any server-wide lock, so an
		// independent connection's command still completes promptly. (Regression guard for the removed
		// m_commandLock that previously serialized — and could deadlock — every connection.)
		var blocked = Host();
		var ex = Assert.Throws<InvalidOperationException>(() =>
			blocked.Invoke<SampleAutomationHost, string>(x => x.BlockFor(10_000), timeoutMs: 250));
		Assert.That(ex!.Message, Does.Contain("timeout"));

		using var other = AppDriver.Attach(m_server!.Port, m_server.Token);
		var element = other.GetElement(x => x.TypeName == "E2EAutomationHost", timeoutMs: 5_000);
		Assert.That(element.Invoke<SampleAutomationHost, string>(x => x.Ping(), timeoutMs: 2_000), Is.EqualTo("pong"));
	}

	[Test]
	public void Connection_WithoutTokenPrefix_IsRejectedAndServerStaysHealthy()
	{
		// An unauthenticated peer that sends a raw line with no token prefix must be rejected before the
		// polymorphic (TypeNameHandling.All) deserializer ever runs on its bytes. The server must reply
		// (rather than hang) and remain healthy for legitimate clients.
		using (var rawClient = new System.Net.Sockets.TcpClient())
		{
			rawClient.Connect("127.0.0.1", m_server!.Port);
			using var stream = rawClient.GetStream();
			var payload = System.Text.Encoding.UTF8.GetBytes("this-line-has-no-token-separator\n");
			stream.Write(payload, 0, payload.Length);
			stream.Flush();

			stream.ReadTimeout = 5_000;
			var buffer = new byte[4096];
			var read = stream.Read(buffer, 0, buffer.Length);
			Assert.That(read, Is.GreaterThan(0), "server should reply to a malformed frame, not hang");
		}

		using var other = AppDriver.Attach(m_server!.Port, m_server.Token);
		Assert.That(other.GetTargets().Select(t => t.TypeName), Does.Contain("E2EAutomationHost"));
	}

	[Test]
	public void GetTargets_ListsRegisteredTarget()
	{
		var targets = m_driver!.GetTargets();
		Assert.That(targets.Select(t => t.TypeName), Does.Contain("E2EAutomationHost"));
	}

	[Test]
	public void Connection_WithWrongToken_IsRejected()
	{
		using var rogue = AppDriver.Attach(m_server!.Port, "wrong-token");
		var ex = Assert.Throws<InvalidOperationException>(() => rogue.GetTargets());
		Assert.That(ex!.Message, Does.Contain("unauthorized"));
	}

	private Element Host() => m_driver!.GetElement(x => x.TypeName == "E2EAutomationHost", timeoutMs: 5_000);

	private SampleAutomationHost m_host = null!;
	private AutomationServer m_server = null!;
	private AppDriver m_driver = null!;
	private string m_targetId = null!;
}
