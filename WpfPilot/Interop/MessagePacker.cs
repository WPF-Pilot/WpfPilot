namespace WpfPilot.Interop;

using System;
using System.IO;
using System.IO.Compression;
using System.Text;
using Newtonsoft.Json;

internal static class MessagePacker
{
	public static string Pack(object message)
	{
		return Compress(JsonConvert.SerializeObject(message, Instance));
	}

	public static dynamic Unpack(string rawMessage)
	{
		return JsonConvert.DeserializeObject(Decompress(rawMessage), Instance)!;
	}

	private static string Compress(string uncompressedString)
	{
		byte[] compressedBytes;

		using (var uncompressedStream = new MemoryStream(Encoding.UTF8.GetBytes(uncompressedString)))
		{
			using var compressedStream = new MemoryStream();
			using (var compressorStream = new DeflateStream(compressedStream, CompressionLevel.Optimal, true))
			{
				uncompressedStream.CopyTo(compressorStream);
			}

			compressedBytes = compressedStream.ToArray();
		}

		return Convert.ToBase64String(compressedBytes);
	}

	private static string Decompress(string compressedString)
	{
		byte[] decompressedBytes;
		var compressedStream = new MemoryStream(Convert.FromBase64String(compressedString));

		using (var decompressorStream = new DeflateStream(compressedStream, CompressionMode.Decompress))
		{
			using var decompressedStream = new MemoryStream();
			decompressorStream.CopyTo(decompressedStream);
			decompressedBytes = decompressedStream.ToArray();
		}

		return Encoding.UTF8.GetString(decompressedBytes);
	}

	private static JsonSerializerSettings Instance { get; } = new JsonSerializerSettings
	{
		ContractResolver = new RobustContractResolver(),
		ConstructorHandling = ConstructorHandling.AllowNonPublicDefaultConstructor,

		// Include type information so we can deserialize to the correct type. Otherwise we could get a long when we wanted an int, etc.
		TypeNameHandling = TypeNameHandling.All,

		// Some apps have a deep object graph.
		MaxDepth = 1000,
	};
}
