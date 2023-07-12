namespace WpfPilot.Injector;

using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;

public static class Injector
{
	public static void InjectAppDriver(WpfProcess process, string pipeName, string dllPath)
	{
		/**
		 * `injector.{architecture}.dll` is loaded into the remote process which then injects and executes the AppDriver automation DLL.
		 * Communication is facilitated by a named pipe which is shared between the remote process and the WpfPilot process.
		 */
		var paths = GetDllPaths(process.Architecture, dllPath);
		var injectorDllName = paths.Item1;
		var injectorDllPath = paths.Item2;
		var appDriverPayloadDllPath = paths.Item3;

		if (!File.Exists(injectorDllPath))
			throw new FileNotFoundException($"Could not find {injectorDllPath}.");
		if (!File.Exists(appDriverPayloadDllPath))
			throw new FileNotFoundException($"Could not find {appDriverPayloadDllPath}.");

		var parameters = new[]
		{
			process.SupportedFrameworkName,
			appDriverPayloadDllPath,
			"WpfPilot.AppDriverPayload.AppDriverPayload",
			"StartAppDriver",
			$"{pipeName}?{dllPath}",
			Path.GetTempFileName(),
		};
		var stringForRemoteProcess = string.Join("<|>", parameters);
		var bufLen = (stringForRemoteProcess.Length + 1) * Marshal.SizeOf(typeof(char));
		var remoteAddress = NativeMethods.VirtualAllocEx(process.Handle, IntPtr.Zero, (uint)bufLen, NativeMethods.AllocationType.Commit | NativeMethods.AllocationType.Reserve, NativeMethods.MemoryProtection.ReadWrite);
		if (remoteAddress == IntPtr.Zero)
			throw new Win32Exception($"Failed to allocate memory in remote process.");

		var address = Marshal.StringToHGlobalUni(stringForRemoteProcess);
		var size = (uint)(sizeof(char) * stringForRemoteProcess.Length);

		try
		{
			var writeProcessMemoryResult = NativeMethods.WriteProcessMemory(process.Handle, remoteAddress, address, size, out var bytesWritten);

			if (writeProcessMemoryResult == false || bytesWritten == 0)
				throw Marshal.GetExceptionForHR(Marshal.GetLastWin32Error()) ?? new InvalidOperationException($"Unknown error while trying to write to foreign process.");

			var hLibrary = IntPtr.Zero;

			try
			{
				// Load library into current process before trying to get the remote proc address
				hLibrary = NativeMethods.LoadLibrary(injectorDllPath);

				// Load library into foreign process before invoking our method
				var moduleHandleInForeignProcess = LoadLibraryInForeignProcess(process, injectorDllPath);

				try
				{
					var remoteProcAddress = NativeMethods.GetRemoteProcAddress(process.Process, injectorDllPath, "ExecuteInDefaultAppDomain");

					if (remoteProcAddress == IntPtr.Zero)
						return;

					var remoteThread = NativeMethods.CreateRemoteThread(process.Handle, IntPtr.Zero, 0, remoteProcAddress, remoteAddress, 0, out _);
					try
					{
						if (remoteThread == IntPtr.Zero)
							throw new Win32Exception($"Failed to create remote thread.");

						NativeMethods.WaitForSingleObject(remoteThread);

						// Get handle of the loaded module
						NativeMethods.GetExitCodeThread(remoteThread, out var resultFromExecuteInDefaultAppDomain);

						if (resultFromExecuteInDefaultAppDomain != IntPtr.Zero)
							throw Marshal.GetExceptionForHR((int)resultFromExecuteInDefaultAppDomain.ToInt64()) ?? new Exception($"Unknown error while executing in foreign process.");
					}
					finally
					{
						NativeMethods.CloseHandle(remoteThread);
					}
				}
				finally
				{
					FreeLibraryInForeignProcess(process, injectorDllName, moduleHandleInForeignProcess);
				}
			}
			finally
			{
				if (hLibrary != IntPtr.Zero)
					NativeMethods.FreeLibrary(hLibrary);

				try
				{
					NativeMethods.VirtualFreeEx(process.Handle, remoteAddress, bufLen, NativeMethods.AllocationType.Release);
				}
				catch (Exception)
				{
				}
			}
		}
		finally
		{
			Marshal.FreeHGlobal(address);
		}
	}

	public static Tuple<string, string, string> GetDllPaths(string architecture, string dllPath)
	{
		var injectorDllName = architecture switch
		{
			"x64" => "injector.x64.dll",
			"x86" => "injector.x86.dll",
			_ => throw new NotImplementedException(architecture),
		};
		var injectorDllPath = Path.Combine(dllPath, $@"WpfPilotResources\{injectorDllName}");
		var appDriverPayloadDllPath = Path.Combine(dllPath, "WpfPilot.dll");

		return new Tuple<string, string, string>(injectorDllName, injectorDllPath, appDriverPayloadDllPath);
	}

	/// <summary>
	/// Loads a library into a foreign process and returns the module handle of the loaded library.
	/// </summary>
	private static IntPtr LoadLibraryInForeignProcess(WpfProcess process, string pathToDll)
	{
		if (File.Exists(pathToDll) == false)
			throw new FileNotFoundException("Could not find file for loading in foreign process.", pathToDll);

		var stringForRemoteProcess = pathToDll;

		var bufLen = (stringForRemoteProcess.Length + 1) * Marshal.SizeOf(typeof(char));
		var remoteAddress = NativeMethods.VirtualAllocEx(process.Handle, IntPtr.Zero, (uint)bufLen, NativeMethods.AllocationType.Commit, NativeMethods.MemoryProtection.ReadWrite);

		if (remoteAddress == IntPtr.Zero)
			throw new Win32Exception($"Failed to allocate memory in remote process.");

		var address = Marshal.StringToHGlobalUni(stringForRemoteProcess);
		var size = (uint)(sizeof(char) * stringForRemoteProcess.Length);

		try
		{
			var writeProcessMemoryResult = NativeMethods.WriteProcessMemory(process.Handle, remoteAddress, address, size, out var bytesWritten);

			if (writeProcessMemoryResult == false || bytesWritten == 0)
				throw Marshal.GetExceptionForHR(Marshal.GetLastWin32Error()) ?? new Exception($"Unknown error while trying to write to foreign process memory.");

			var hLibrary = NativeMethods.GetModuleHandle("kernel32");

			// Load dll into the remote process
			// (via CreateRemoteThread & LoadLibrary)
			var procAddress = NativeMethods.GetProcAddress(hLibrary, "LoadLibraryW");

			if (procAddress == IntPtr.Zero)
				throw new Win32Exception($"Failed to get proc address for LoadLibraryW.");

			var remoteThread = NativeMethods.CreateRemoteThread(process.Handle,
				IntPtr.Zero,
				0,
				procAddress,
				remoteAddress,
				0,
				out _);

			IntPtr moduleHandleInForeignProcess;
			try
			{
				if (remoteThread == IntPtr.Zero)
				{
					throw new Win32Exception($"Failed to create remote thread.");
				}
				else
				{
					NativeMethods.WaitForSingleObject(remoteThread);

					// Get handle of the loaded module
					if (NativeMethods.GetExitCodeThread(remoteThread, out moduleHandleInForeignProcess) == false)
						throw new Win32Exception($"Failed to get exit code thread.");
				}
			}
			finally
			{
				NativeMethods.CloseHandle(remoteThread);
			}

			try
			{
				NativeMethods.VirtualFreeEx(process.Handle, remoteAddress, bufLen, NativeMethods.AllocationType.Release);
			}
			catch (Exception)
			{
			}

			if (moduleHandleInForeignProcess == IntPtr.Zero)
				throw new Exception($"Could not load \"{pathToDll}\" in process \"{process.Id}\".");

			var remoteHandle = NativeMethods.GetRemoteModuleHandle(process.Process, Path.GetFileName(pathToDll));
			return remoteHandle;
		}
		finally
		{
			Marshal.FreeHGlobal(address);
		}
	}

	/// <summary>
	/// Frees a library in a foreign process.
	/// </summary>
	private static bool FreeLibraryInForeignProcess(WpfProcess process, string moduleName, IntPtr moduleHandleInForeignProcess)
	{
		var hLibrary = NativeMethods.GetModuleHandle("kernel32");
		var procAddress = NativeMethods.GetProcAddress(hLibrary, "FreeLibraryAndExitThread");
		if (procAddress == IntPtr.Zero)
		{
			// todo: error handling
		}

		var remoteThread = NativeMethods.CreateRemoteThread(process.Handle,
			IntPtr.Zero,
			0,
			procAddress,
			moduleHandleInForeignProcess,
			0,
			out _);
		try
		{
			if (remoteThread == IntPtr.Zero)
			{
				return false;
			}

			NativeMethods.WaitForSingleObject(remoteThread);
		}
		finally
		{
			NativeMethods.CloseHandle(remoteThread);
		}

		return true;
	}
}