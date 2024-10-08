namespace WpfPilot.AppDriverPayload;

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using WpfPilot.AppDriverPayload.Commands;
using WpfPilot.Interop;
using WpfPilot.Utility;
using WpfPilot.Utility.WpfUtility;
using WpfPilot.Utility.WpfUtility.Tree;
using static WpfPilot.Utility.ThreadUtility;
using Command = WpfPilot.Interop.NamedPipeServer.Command;

public static class AppDriverPayload
{
	public static int StartAppDriver(string args)
	{
		Log.Info($"Loading {nameof(AppDriverPayload)} into target process.");

		// Eg `pid-123-{guid}?C:\code\WpfApp\TestSuite\`
		var split = args.Split('?');
		var pipeName = split[0];
		var dllPath = split[1];

		var testRun = RunOnUIThread(rootObject => Task.FromResult(true));
		if (testRun == UIThreadRunResult.Unable)
			Exit("Injected into a non-UI thread. This could happen if the app has a boot up screen phase, or the like.");

		try
		{
			AppDomain.CurrentDomain.AssemblyResolve += (object? sender, ResolveEventArgs args) =>
				AssemblyResolver(sender, args, dllPath);

			AppDomain.CurrentDomain.UnhandledException += ExceptionLog.Handler($"{pipeName}-crash.txt");
		}
		catch (Exception e)
		{
			Exit($"Failed to hook AssemblyResolve in {nameof(StartAppDriver)}\n{e}");
		}

		LoadDependencies(dllPath);

		var thread = new Thread(() => CommandLoop(pipeName))
		{
			IsBackground = true
		};
		thread.Start();

		Log.Info($"Successfully injected {nameof(AppDriverPayload)} into App");
		return 0;
	}

	private static void CommandLoop(string pipeName)
	{
		Log.Info($"Processing Commands in {nameof(CommandLoop)} using pipe `{pipeName}`");

		var channel = new NamedPipeServer(pipeName);

		// Due to re-entry, we need to lock the loop to ensure we don't process multiple commands at once.
		var loopLock = new object();

		while (true)
		{
			lock (loopLock)
			{
				var command = channel.WaitForNextCommand();

				var cts = new CancellationTokenSource();

				// Check if a dialog was opened with `ShowDialog` and early return if so, as the main window will be blocked.
				Task<UIThreadRunResult> showDialogCheckerTask = RunOnStaThread(async () => await CheckIfShowDialogCalled(cts.Token));

				Task<UIThreadRunResult> ranOnUIThreadTask = Task.Run(() => RunOnUIThread(async rootObject =>
				{
					var propNames = command.Value?.Kind == nameof(GetVisualTreeCommand) ?
						PropInfo.GetPropertyValue(command.Value, "PropNames") :
						new HashSet<string>();

					var treeService = new TreeService(propNames ?? new HashSet<string>());
					treeService.Construct(rootObject, null, omitChildren: false);

					AppHooks.EnsureHooked();

					try
					{
						await ProcessCommand(command, treeService);
					}
					catch (Exception e)
					{
						command.Respond(new { Error = e.ToString() });
					}
				}));

				var firstCompleted = Task.WhenAny(ranOnUIThreadTask, showDialogCheckerTask).GetAwaiter().GetResult();
				var result = firstCompleted.GetAwaiter().GetResult();

				// Ensure task is cleaned up before continuing.
				cts.Cancel();
				showDialogCheckerTask?.GetAwaiter().GetResult();
				AppHooks.ShowDialogCalled = false;

				// Lost access to UI thread, so we need to reinject or exit.
				if (result == UIThreadRunResult.Unable)
				{
					Log.Info("Lost access to UI thread. Closing command loop.");
					channel.Dispose();
					return;
				}

				var isHangCommand = command.Value.Kind != nameof(GetVisualTreeCommand) && command.Value.Kind != nameof(ScreenshotCommand);
				if (result == UIThreadRunResult.Pending && !command.CheckHasResponded() && isHangCommand)
					command.Respond(new { Value = "UnserializableResult" });

				// Even though ranOnUIThread has returned, there may still be async work to do, since it will return when the first await is hit, NOT when the async work is done.
				// We are only finished when `command.CheckHasResponded()` is true.
				while (!command.CheckHasResponded())
					Task.Delay(10).GetAwaiter().GetResult();
			}
		}
	}

	internal static async Task ProcessCommand(Command command, TreeService treeService)
	{
		switch (command.Value.Kind)
		{
			case nameof(InvokeCommand):
				Log.Info("Processing Invoke command", command.Value);
				await InvokeCommand.ProcessAsync(command, treeService.RootTreeItem);
				break;
			case nameof(InvokeStaticCommand):
				Log.Info("Processing InvokeStatic command", command.Value);
				await InvokeStaticCommand.ProcessAsync(command);
				break;
			case nameof(RaiseEventCommand):
				Log.Info("Processing RaiseEvent command", command.Value);
				RaiseEventCommand.Process(command, treeService.RootTreeItem);
				break;
			case nameof(GetVisualTreeCommand):
				Log.Info("Processing GetVisualTree command");
				GetVisualTreeCommand.Process(command, treeService);
				break;
			case nameof(SetPropertyCommand):
				Log.Info("Processing SetProperty command", command.Value);
				SetPropertyCommand.Process(command, treeService.RootTreeItem);
				break;
			case nameof(ClickCommand):
				Log.Info("Processing Click command", command.Value);
				ClickCommand.Process(command, treeService.RootTreeItem);
				break;
			case nameof(ScreenshotCommand):
				Log.Info("Processing Screenshot command", command.Value);
				ScreenshotCommand.Process(command, treeService.RootTreeItem);
				break;
			default:
				Log.Info($"Unsupported command kind received: {command.Value.Kind}");
				command.Respond(new { Error = $"Unsupported command kind: {command.Value.Kind}" });
				break;
		}
	}

	internal static async Task<UIThreadRunResult> CheckIfShowDialogCalled(CancellationToken token)
	{
		while (!AppHooks.ShowDialogCalled)
		{
			if (token.IsCancellationRequested)
				return UIThreadRunResult.Finished;

			try
			{
				await Task.Delay(50, token);
			}
			catch (TaskCanceledException)
			{
				return UIThreadRunResult.Finished;
			}
		}

		return UIThreadRunResult.Pending;
	}

	private static Assembly? AssemblyResolver(object? sender, ResolveEventArgs args, string dllPath)
	{
		// Maintainers note: do not add any references to `Newtonsoft.Json` (for example, to log a message) in this block or it could trigger an infinite recursion exception.
		// This is because Newtonsoft.Json itself may have not yet been loaded, so referencing it will trigger this block of code again and again.

		if (args.Name?.StartsWith("WpfPilot,") == true)
			return Assembly.GetExecutingAssembly();

		/**
		 * For a primer on DLL resolving see:
		 * https://learn.microsoft.com/en-us/dotnet/core/dependency-loading/default-probing (Core)
		 * https://github.com/dotnet/cli/blob/rel/1.0.0/Documentation/specs/runtime-configuration-file.md (Core)
		 * https://learn.microsoft.com/en-us/dotnet/framework/deployment/how-the-runtime-locates-assemblies (Framework)
		 * There are some differences between .NET Core and .NET Framework, but the general ideas are the same.
		 * `Assembly.LoadFile` is used instead of `Assembly.(Unsafe)LoadFrom` because the latter throws `FileLoadException` for a small percentage of assemblies, particularly `System.*` assemblies.
		 */
		Assembly Load(string dll) => Assembly.LoadFile(Path.Combine(dllPath, dll));

		if (args.Name?.StartsWith("Newtonsoft.Json,") == true)
			return Load("Newtonsoft.Json.dll");

		if (args.Name?.StartsWith("System.ValueTuple,") == true)
			return Load("System.ValueTuple.dll");

		return null;
	}

	private static void LoadDependencies(string dllPath)
	{
		/**
		 * Some DLLs must be loaded via the `AssemblyResolver` method, others must be loaded following the below pattern.
		 * It is not known why some DLLs require `AssemblyResolver` and others require `Assembly.UnsafeLoadFrom`.
		 * Some may even require a combination of both.
		 *
		 * GUIDELINES WHEN ADDING A NEW DEPENDENCY
		 * - Choose the minimal version of the dependency that works. For example `System.Drawing.Common` 4.7.2 is used instead of 7.0.0.
		 * This is because the injected app may be using a lower version of the dependency, if version 7.0.0 is used when the app is on 6.0.0, then an exception will be thrown.
		 * However if version 4.7.2 is used when the app is on 6.0.0, no exception will be thrown and 6.0.0 will be used.
		 * Patch version differences seem to load fine. For example if the app is on `Newtonsoft.Json` 13.0.1 and we're on 13.0.2, there is no exception thrown.
		 * - Handle and report version mismatches to the user gracefully.
		 * - Avoid dependencies in general.
		 */
		void Load(string dll) => TryUnsafeLoadFrom(Path.Combine(dllPath, dll));
		Load("0Harmony.dll");
		Load("Newtonsoft.Json.dll");
		Load("System.ValueTuple.dll"); // Only needed for .NET Framework.
	}

	private static void TryUnsafeLoadFrom(string assemblyPath)
	{
		try
		{
			Assembly.UnsafeLoadFrom(assemblyPath);
		}
		catch
		{
			// The assembly is unsupported on this platform or the injected app has its own conflicting version.
			// Sometimes the `dotnet` assemblies do not have the `netframework` assemblies, or vice versa.
		}
	}

	// HACK: For some reason we cannot exit as failure using `return 1;` and have to throw an exception instead.
	// Otherwise, the injector executable treats the exit code as success.
	private static void Exit(string message)
	{
		Log.Info(message);
		throw new InvalidOperationException(message);
	}
}
