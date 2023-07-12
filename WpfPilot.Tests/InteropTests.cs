namespace WpfPilot.Tests;

using System.Threading;
using NUnit.Framework;
using WpfPilot.Interop;

[TestFixture]
public sealed class InteropTests
{
	[Test]
	public void TestInteropPipe()
	{
		dynamic? response = null;
		var clientThread = new Thread(() =>
		{
			using var client = new NamedPipeClient(nameof(InteropTests), () => false, () => { });
			response = client.GetResponse(new { Command = "Ping\n123", TheInt = 1, TheDouble = 3.0 });
		});
		var serverThread = new Thread(() =>
		{
			while (true)
			{
				using var server = new NamedPipeServer(nameof(InteropTests));
				var command = server.WaitForNextCommand();

				Assert.AreEqual(1, command.Value.TheInt);
				Assert.AreEqual(typeof(int), command.Value.TheInt.GetType()); // Type should not be converted to long.

				Assert.AreEqual(3.0, command.Value.TheDouble);
				Assert.AreEqual(typeof(double), command.Value.TheDouble.GetType());

				command.Respond(new { Result = "Pong\n456" });
			}
		});
		clientThread.IsBackground = true;
		serverThread.IsBackground = true;

		clientThread.Start();
		serverThread.Start();
		clientThread.Join();

		Assert.IsNotNull(response);
		Assert.AreEqual("Pong\n456", response!.Result);
	}

	[Test]
	public void TestSerializingObjectsWithPrivateConstructors()
	{
		Assert.DoesNotThrow(() =>
		{
			var packed = MessagePacker.Pack(ExampleClass.Create());
			var unpacked = MessagePacker.Unpack(packed);
			Assert.AreEqual(123, unpacked.Value);
		});
	}

	public class ExampleClass
	{
		public static ExampleClass Create()
		{
			return new ExampleClass(123);
		}

		private ExampleClass(int value)
		{
			Value = value;
		}

		public int Value { get; }
	}
}
