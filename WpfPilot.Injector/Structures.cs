namespace WpfPilot.Injector;

using System;
using System.Runtime.InteropServices;

#pragma warning disable

[StructLayout(LayoutKind.Sequential)]
internal struct POINT
{
	internal int X;
	internal int Y;
}

[StructLayout(LayoutKind.Sequential)]
internal struct HARDWAREINPUT
{
	internal uint uMsg;
	internal ushort wParamL;
	internal ushort wParamH;
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

#pragma warning restore
