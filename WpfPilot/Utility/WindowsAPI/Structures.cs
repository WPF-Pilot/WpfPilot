namespace WpfPilot.Utility.WindowsAPI;

using System;
using System.Drawing;
using System.Runtime.InteropServices;

#pragma warning disable

[StructLayout(LayoutKind.Sequential)]
internal struct POINT
{
	internal int X;
	internal int Y;
}

[StructLayout(LayoutKind.Sequential)]
internal struct COLORREF
{
	internal byte R;
	internal byte G;
	internal byte B;

	internal COLORREF(byte r, byte g, byte b)
	{
		R = r;
		G = g;
		B = b;
	}

	public static implicit operator Color(COLORREF c)
	{
		return Color.FromArgb(c.R, c.G, c.B);
	}

	public static implicit operator COLORREF(Color c)
	{
		return new COLORREF(c.R, c.G, c.B);
	}

#if NETFRAMEWORK
	public static implicit operator System.Windows.Media.Color(COLORREF c)
	{
		return System.Windows.Media.Color.FromRgb(c.R, c.G, c.B);
	}

	public static implicit operator COLORREF(System.Windows.Media.Color c)
	{
		return new COLORREF(c.R, c.G, c.B);
	}
#endif

	public override string ToString()
	{
		return $"R={R},G={G},B={B}";
	}
}

[StructLayout(LayoutKind.Sequential)]
internal struct INPUT
{
	internal InputType type;
	internal INPUTUNION u;

	internal static int Size => Marshal.SizeOf(typeof(INPUT));

	internal static INPUT MouseInput(MOUSEINPUT mouseInput)
	{
		return new INPUT { type = InputType.INPUT_MOUSE, u = new INPUTUNION { mi = mouseInput } };
	}

	internal static INPUT KeyboardInput(KEYBDINPUT keyboardInput)
	{
		return new INPUT { type = InputType.INPUT_KEYBOARD, u = new INPUTUNION { ki = keyboardInput } };
	}

	internal static INPUT HardwareInput(HARDWAREINPUT hardwareInput)
	{
		return new INPUT { type = InputType.INPUT_HARDWARE, u = new INPUTUNION { hi = hardwareInput } };
	}
}

[StructLayout(LayoutKind.Explicit)]
internal struct INPUTUNION
{
	[FieldOffset(0)] internal MOUSEINPUT mi;

	[FieldOffset(0)] internal KEYBDINPUT ki;

	[FieldOffset(0)] internal HARDWAREINPUT hi;
}

[StructLayout(LayoutKind.Sequential)]
internal struct MOUSEINPUT
{
	internal int dx;
	internal int dy;
	internal uint mouseData;
	internal MouseEventFlags dwFlags;
	internal uint time;
	internal IntPtr dwExtraInfo;
}

[StructLayout(LayoutKind.Sequential)]
internal struct KEYBDINPUT
{
	internal ushort wVk;
	internal ushort wScan;
	internal KeyEventFlags dwFlags;
	internal uint time;
	internal IntPtr dwExtraInfo;
}

[StructLayout(LayoutKind.Sequential)]
internal struct HARDWAREINPUT
{
	internal uint uMsg;
	internal ushort wParamL;
	internal ushort wParamH;
}

[StructLayout(LayoutKind.Sequential)]
internal struct CURSORINFO
{
	internal int cbSize;
	internal CursorState flags;
	internal IntPtr hCursor;
	internal POINT ptScreenPos;
}

[StructLayout(LayoutKind.Sequential)]
internal struct ICONINFO
{
	internal bool fIcon;
	internal int xHotspot;
	internal int yHotspot;
	internal IntPtr hbmMask;
	internal IntPtr hbmColor;
}

[StructLayout(LayoutKind.Sequential)]
internal struct RECT
{
	internal int left;
	internal int top;
	internal int right;
	internal int bottom;

	public override string ToString()
	{
		return $"{{X={left},Y={top},Width={right - left},Height={bottom - top}}}";
	}
}

[StructLayout(LayoutKind.Sequential)]
internal struct MonitorInfo
{
	internal uint size;
	internal RECT monitor;
	internal RECT work;
	internal uint flags;
}

[StructLayout(LayoutKind.Sequential)]
internal struct POINTER_TOUCH_INFO
{
	internal POINTER_INFO pointerInfo;
	internal TouchFlags touchFlags;
	internal TouchMask touchMask;
	internal RECT rcContact;
	internal RECT rcContactRaw;
	internal uint orientation;
	internal uint pressure;
}

[StructLayout(LayoutKind.Sequential)]
internal struct POINTER_INFO
{
	internal PointerInputType pointerType;
	internal uint pointerId;
	internal uint frameId;
	internal PointerFlags pointerFlags;
	internal IntPtr sourceDevice;
	internal IntPtr hwndTarget;
	internal POINT ptPixelLocation;
	internal POINT ptPixelLocationRaw;
	internal POINT ptHimetricLocation;
	internal POINT ptHimetricLocationRaw;
	internal uint dwTime;
	internal uint historyCount;
	internal uint inputData;
	internal uint dwKeyStates;
	internal ulong PerformanceCount;
	internal PointerButtonChangeType ButtonChangeType;
}
#pragma warning restore
