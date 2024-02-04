namespace WpfPilot.Injector;

using System;
using System.Diagnostics;

/**
 * The WPF Injector application injects the `AppDriverPayload` into the target process.
 * It is a separate application because injection requires the injector be the same architecture as the target process.
 * For example, an x86 application must be injected with an x86 injector.
 */
internal sealed class Program
{
	public static void Main(string[] args)
	{
		if (args.Length != 1)
		{
			Console.Error.WriteLine("Usage: WpfPilot.Injector.exe <pipeName>?<dllPath>?<processId>");
			Environment.Exit(1);
		}

		args = args[0].Split('?');
		var pipeName = args[0]; // Eg `pid-123-{guid}`
		var dllRootDirectory = args[1]; // Eg `C:\code\WpfApp\TestSuite\`
		var processId = int.Parse(args[2]); // Eg `123`

		try
		{
			var process = new WpfProcess(Process.GetProcessById(processId));
			Injector.InjectAppDriver(process, pipeName, dllRootDirectory);
		}
		catch (Exception e)
		{
			Console.Error.WriteLine(e);
			Environment.Exit(1);
		}
	}
}
