namespace WpfPilot;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;
using WpfPilot.Utility;
using WpfPilot.Utility.WindowsAPI;

public sealed class Keyboard
{
	internal Keyboard(Action onAction)
	{
		OnAction = onAction ?? throw new ArgumentNullException(nameof(onAction));
	}

	/// <summary>
	/// Sends a physical key press to the active application with full modifier key support.
	/// When modifier keys (Ctrl, Alt, Shift) are encountered, they remain held for subsequent keys.
	/// <code>
	/// appDriver.Keyboard.Press(Key.LeftCtrl, Key.A); // Ctrl+A to select all
	/// appDriver.Keyboard.Press(Key.LeftAlt, Key.F); // Alt+F to open File menu
	/// appDriver.Keyboard.Press(Key.LeftCtrl, Key.LeftShift, Key.Tab); // Ctrl+Shift+Tab
	/// </code>
	/// </summary>
	public void Press(params Key[] keys)
	{
		var heldModifiers = new HashSet<Key>();

		foreach (var key in keys)
		{
			if (IsModifierKey(key))
			{
				if (!heldModifiers.Contains(key))
				{
					// Press the modifier
					var modifierCode = (byte) KeyInterop.VirtualKeyFromKey(key);
					SendInput(modifierCode, true, false, false, false);
					heldModifiers.Add(key);
				}

				continue;
			}

			var code = KeyInterop.VirtualKeyFromKey(key);
			if (code == -1)
				throw new ArgumentException($"Invalid key: {key}");

			var virtualKey = (byte) (code & 0xff);

			// Press and release the regular key
			SendInput(virtualKey, true, false, false, false);
			SendInput(virtualKey, false, false, false, false);
		}

		// Release all held modifiers in reverse order
		foreach (var modifier in heldModifiers.Reverse())
		{
			var modifierCode = (byte) KeyInterop.VirtualKeyFromKey(modifier);
			SendInput(modifierCode, false, false, false, false);
		}

		OnAction();
	}

	/// <summary>
	/// Sends a physical key press to the active application.
	/// Handles shift state for special characters and capital letters.
	/// <code>
	/// appDriver.GetElement(x => x["Name"] == "FileInput").Click();
	/// appDriver.Keyboard.PhysicalType("C:\code\myfile.txt");
	/// appDriver.Keyboard.PhysicalPress(Key.Enter);
	/// </code>
	/// </summary>
	public void Type(string text)
	{
		foreach (var key in text)
		{
			var virtualKeyCode = User32.VkKeyScan(key);

			// The high byte contains shift state information
			var shiftState = (byte) ((virtualKeyCode >> 8) & 0xFF);

			// The low byte contains the virtual key code
			var vkCode = (byte) (virtualKeyCode & 0xFF);

			var needsShift = false;

			// Check if shift is needed based on the shift state
			// Bit 0: Shift key
			// Bit 1: Ctrl key
			// Bit 2: Alt key
			if ((shiftState & 1) != 0)
			{
				needsShift = true;
			}

			// Handle shift key if needed
			if (needsShift)
			{
				// Press shift down
				SendInput(User32.VK_SHIFT, true, false, false, false);
			}

			// Press and release the actual key
			SendInput(vkCode, true, false, false, false);
			SendInput(vkCode, false, false, false, false);

			if (needsShift)
			{
				// Release shift
				SendInput(User32.VK_SHIFT, false, false, false, false);
			}
		}

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

	private bool IsModifierKey(Key key)
	{
		return key == Key.LeftCtrl || key == Key.RightCtrl ||
			   key == Key.LeftAlt || key == Key.RightAlt ||
			   key == Key.LeftShift || key == Key.RightShift ||
			   key == Key.LWin || key == Key.RWin;
	}

	private Action OnAction { get; }
}
