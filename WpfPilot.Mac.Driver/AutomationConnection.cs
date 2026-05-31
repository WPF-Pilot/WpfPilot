namespace WpfPilot.Mac.Driver;

using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Threading;
using WpfPilot.Mac.Protocol;
using WpfPilot.Mac.Transport;

/// <summary>
/// A single authenticated loopback connection from the test-side driver to an in-process
/// <c>AutomationServer</c>. Requests are serialized so responses match their requests.
/// </summary>
internal sealed class AutomationConnection : IDisposable
{
	public AutomationConnection(int port, string token)
	{
		m_token = token;
		m_client = new TcpClient();
		m_client.Connect("127.0.0.1", port);
		m_stream = m_client.GetStream();
	}

	public ResponseMessage Send(CommandMessage command, int timeoutMs)
	{
		lock (m_lock)
		{
			m_stream.ReadTimeout = timeoutMs <= 0 ? Timeout.Infinite : timeoutMs + 5_000;
			FrameProtocol.WriteFrame(m_stream, m_token, command);
			var response = FrameProtocol.ReadFrame<ResponseMessage>(m_stream);
			if (response is null)
				throw new InvalidOperationException("Automation server closed the connection.");
			return response;
		}
	}

	public void Dispose()
	{
		try
		{
			m_stream.Dispose();
		}
		catch
		{
			// ignore
		}

		m_client.Dispose();
	}

	private readonly string m_token;
	private readonly TcpClient m_client;
	private readonly NetworkStream m_stream;
	private readonly object m_lock = new();
}
