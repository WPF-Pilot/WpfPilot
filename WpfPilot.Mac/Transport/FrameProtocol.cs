namespace WpfPilot.Mac.Transport;

using System;
using System.IO;
using System.Text;
using WpfPilot.Interop;

/// <summary>
/// Frames messages over a duplex stream the same way the Windows WpfPilot named-pipe transport does:
/// each frame is <c>MessagePacker.Pack(obj)</c> (Deflate + Base64 of Newtonsoft JSON) followed by a
/// single <c>'\n'</c>. Base64 never contains a newline, so the delimiter is unambiguous.
/// </summary>
/// <remarks>
/// Request frames carry the per-launch auth token as a plaintext prefix (<c>token|payload</c>) so the
/// server can authenticate <em>before</em> running the polymorphic (<c>TypeNameHandling.All</c>)
/// deserializer on attacker-controlled bytes. The token alphabet (hex) and the Base64 payload alphabet
/// both exclude <c>'|'</c>, so splitting on the first <c>'|'</c> is unambiguous. Response frames flow
/// server-to-client over the already-authenticated socket and carry no token prefix.
/// </remarks>
public static class FrameProtocol
{
	/// <summary>Maximum accepted frame size; guards against an unauthenticated peer streaming bytes
	/// without a newline to exhaust server memory.</summary>
	private const int MaxFrameBytes = 16 * 1024 * 1024;

	private const char TokenSeparator = '|';

	/// <summary>Writes an unauthenticated frame (used for server-to-client responses).</summary>
	public static void WriteFrame(Stream stream, object message)
	{
		WriteRaw(stream, MessagePacker.Pack(message));
	}

	/// <summary>Writes a token-prefixed request frame so the peer can authenticate before deserializing.</summary>
	public static void WriteFrame(Stream stream, string token, object message)
	{
		WriteRaw(stream, token + TokenSeparator + MessagePacker.Pack(message));
	}

	/// <summary>Reads a single frame and deserializes it to <typeparamref name="T"/>. Returns null at end of stream.</summary>
	public static T? ReadFrame<T>(Stream stream)
		where T : class
	{
		var line = ReadLine(stream);
		if (line is null)
			return null;
		return MessagePacker.Unpack(line) as T;
	}

	/// <summary>
	/// Reads a request frame without deserializing its payload, returning the plaintext token prefix and
	/// the raw (still-packed) payload so the caller can authenticate before invoking the deserializer.
	/// </summary>
	public static RawFrame ReadRawFrame(Stream stream)
	{
		var line = ReadLine(stream);
		if (line is null)
			return new RawFrame { EndOfStream = true };

		var sep = line.IndexOf(TokenSeparator);
		if (sep < 0)
			return new RawFrame { Token = null, Payload = null };

		return new RawFrame { Token = line.Substring(0, sep), Payload = line.Substring(sep + 1) };
	}

	/// <summary>Deserializes a raw (post-authentication) payload to <typeparamref name="T"/>.</summary>
	public static T? Unpack<T>(string payload)
		where T : class
	{
		return MessagePacker.Unpack(payload) as T;
	}

	private static void WriteRaw(Stream stream, string line)
	{
		var bytes = Encoding.UTF8.GetBytes(line + "\n");
		stream.Write(bytes, 0, bytes.Length);
		stream.Flush();
	}

	private static string? ReadLine(Stream stream)
	{
		var buffer = new MemoryStream();
		var one = new byte[1];
		while (true)
		{
			var read = stream.Read(one, 0, 1);
			if (read == 0)
				return buffer.Length == 0 ? null : Encoding.UTF8.GetString(buffer.ToArray());
			if (one[0] == (byte) '\n')
				return Encoding.UTF8.GetString(buffer.ToArray());
			if (buffer.Length >= MaxFrameBytes)
				throw new IOException($"Automation frame exceeded the maximum size of {MaxFrameBytes} bytes.");
			buffer.WriteByte(one[0]);
		}
	}

	/// <summary>A request frame read without deserializing its payload.</summary>
	public readonly struct RawFrame
	{
		/// <summary>True when the peer closed the connection with no pending frame.</summary>
		public bool EndOfStream { get; init; }

		/// <summary>The plaintext token prefix, or null when the frame is malformed (no separator).</summary>
		public string? Token { get; init; }

		/// <summary>The raw, still-packed payload to deserialize once authenticated.</summary>
		public string? Payload { get; init; }
	}
}
