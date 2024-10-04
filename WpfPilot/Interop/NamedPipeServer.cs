namespace WpfPilot.Interop;

using System;
using System.IO;
using System.IO.Pipes;

internal sealed class NamedPipeServer : IDisposable
{
	public NamedPipeServer(string pipeName)
	{
		Pipe = new NamedPipeServerStream(
			pipeName: pipeName,
			direction: PipeDirection.InOut,
			maxNumberOfServerInstances: 1,
			transmissionMode: PipeTransmissionMode.Message);
	}

	public void Dispose()
	{
		Pipe.Dispose();
	}

	public Command WaitForNextCommand()
	{
		if (!Pipe.IsConnected)
			Pipe.WaitForConnection();

		var reader = new StreamReader(Pipe);
		var clientCommandString = reader.ReadLine();
		var hasResponded = false;

		bool CheckHasResponded()
		{
			return hasResponded;
		}

		void Respond(dynamic response)
		{
			if (hasResponded)
				return;
			var writer = new StreamWriter(Pipe);
			var r = MessagePacker.Pack(response);
			writer.Write(r);
			writer.Write('\n');
			writer.Flush();
			hasResponded = true;
		}

		return new Command
		{
			Value = MessagePacker.Unpack(clientCommandString),
			Respond = Respond,
			CheckHasResponded = CheckHasResponded,
		};
	}

	public struct Command
	{
		public dynamic Value { get; set; }
		public Action<dynamic> Respond { get; set; }
		public Func<bool> CheckHasResponded { get; set; }
	}

	private NamedPipeServerStream Pipe { get; }
}
