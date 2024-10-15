namespace WpfPilot;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Windows;
using System.Windows.Input;
using WpfPilot.AppDriverPayload;
using WpfPilot.AppDriverPayload.Commands;
using WpfPilot.Interop;
using WpfPilot.Utility;
using WpfPilot.Utility.WindowsAPI;

public sealed class Keyboard
{
	internal Keyboard(NamedPipeClient channel, Process process, Action onAction)
	{
		Channel = channel ?? throw new ArgumentNullException(nameof(channel));
		OnAction = onAction ?? throw new ArgumentNullException(nameof(onAction));
		Process = process ?? throw new ArgumentNullException(nameof(process));
	}

	/// <summary>
	/// Sends a key press message to the application. For modifier key support such as `Ctrl` and `Shift`, use `Keyboard.Hotkey` instead.
	/// <code>
	/// ✏️ appDriver.Keyboard.Press(Key.A, Key.B, Key.C);
	/// </code>
	/// </summary>
	public void Press(params Key[] keys)
	{
		// Not the most kosher implementation, sorry Raymond, but it works fine for WPF apps.
		// Note: `Hotkey` uses `SendInput` because that's the only simple way to send modifier keys.
		foreach (var key in keys)
			User32.PostMessage(Process.MainWindowHandle, WindowsMessages.WM_CHAR, (IntPtr) KeyInterop.VirtualKeyFromKey(key), IntPtr.Zero);

		OnAction();
	}

	/// <summary>
	/// Sends a physical key press to the active application.
	/// Prefer using `Press` or `Hotkey`, but `PhysicalPress` can be useful for `MessageBox` scenarios.
	/// <code>
	/// ✏️ appDriver.Keyboard.PhysicalPress(Key.Tab, Key.Enter);
	/// </code>
	/// </summary>
	public void PhysicalPress(params Key[] keys)
	{
		foreach (var key in keys)
		{
			var code = KeyInterop.VirtualKeyFromKey(key);
			if (code == -1)
				throw new ArgumentException($"Invalid key: {key}");

			var low = (byte) (code & 0xff);

			// Type the effective key
			SendInput(low, true, false, false, false);
			SendInput(low, false, false, false, false);
		}

		OnAction();
	}

	/// <summary>
	/// Triggers a `TextCompositionEventArgs` on the currently focused element. This simulates typing on a keyboard.
	/// <code>
	/// ✏️ appDriver.Keyboard.Type("Hello world!");
	/// </code>
	/// </summary>
	public void Type(string text)
	{
		Expression<Action<Application>> code = _ => KeyboardInput.Type(text);
		var response = Channel.GetResponse(new
		{
			Kind = nameof(InvokeStaticCommand),
			Code = Eval.SerializeCode(code),
		});

		OnAction();
	}

	/// <summary>
	/// Sends a key press input to the application. Due to implementation details, this will bring the application to the foreground.<br/>
	/// If modifier key support is unneeded, consider using `Press` instead.
	/// <code>
	/// ✏️ appDriver.Keyboard.Hotkey(ModifierKeys.Control, Key.A);
	/// </code>
	/// </summary>
	public void Hotkey(ModifierKeys modifier, Key key)
	{
		var code = KeyInterop.VirtualKeyFromKey(key);
		if (code == -1)
			throw new ArgumentException($"Invalid key: {key}");

		var low = (byte) (code & 0xff);

		// Check if there are any modifiers
		var modifiers = new List<VirtualKeyShort>();

		if (modifier.HasFlag(ModifierKeys.Shift))
			modifiers.Add(VirtualKeyShort.SHIFT);

		if (modifier.HasFlag(ModifierKeys.Control))
			modifiers.Add(VirtualKeyShort.CONTROL);

		if (modifier.HasFlag(ModifierKeys.Alt))
			modifiers.Add(VirtualKeyShort.ALT);

		if (modifier.HasFlag(ModifierKeys.Windows))
			modifiers.Add(VirtualKeyShort.LWIN);

		// Set the foreground window to the app process, so it receives input.
		var currentForegroundWindow = User32.GetForegroundWindow();
		User32.SetForegroundWindow(Process.MainWindowHandle);

		// Press the modifiers
		foreach (var mod in modifiers)
			SendInput((ushort) mod, true, false, false, false);

		// Type the effective key
		SendInput(low, true, false, false, false);
		SendInput(low, false, false, false, false);

		// Release the modifiers
		foreach (var mod in Enumerable.Reverse(modifiers))
			SendInput((ushort) mod, false, false, false, false);

		// Restore the foreground window.
		User32.SetForegroundWindow(currentForegroundWindow);

		OnAction();
	}

	// From FlaUI.
	private static void SendInput(ushort keyCode, bool isKeyDown, bool isScanCode, bool isExtended, bool isUnicode)
	{
		// Prepare the basic object
		var keyboardInput = new KEYBDINPUT
		{
			time = 0,
			dwExtraInfo = User32.GetMessageExtraInfo()
		};

		// Add the "key-up" flag if needed. By default it is "key-down"
		if (!isKeyDown)
			keyboardInput.dwFlags |= KeyEventFlags.KEYEVENTF_KEYUP;

		if (isScanCode)
		{
			keyboardInput.wScan = keyCode;
			keyboardInput.dwFlags |= KeyEventFlags.KEYEVENTF_SCANCODE;

			// Add the extended flag if the flag is set or the keycode is prefixed with the byte 0xE0
			// See https://msdn.microsoft.com/en-us/library/windows/desktop/ms646267(v=vs.85).aspx
			if (isExtended || (keyCode & 0xFF00) == 0xE0)
				keyboardInput.dwFlags |= KeyEventFlags.KEYEVENTF_EXTENDEDKEY;
		}
		else if (isUnicode)
		{
			keyboardInput.dwFlags |= KeyEventFlags.KEYEVENTF_UNICODE;
			keyboardInput.wScan = keyCode;
		}
		else
		{
			keyboardInput.wVk = keyCode;
		}

		// Build the input object
		var input = INPUT.KeyboardInput(keyboardInput);

		// Send the command
		if (User32.SendInput(1, new[] { input }, INPUT.Size) == 0)
			Log.Info("Failed to send input");
	}

	private NamedPipeClient Channel { get; }
	private Action OnAction { get; }
	private Process Process { get; }
}
