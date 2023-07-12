namespace WpfPilot.Utility.WindowsAPI;

using System;

internal static class Delegates
{
	public delegate bool MonitorEnumDelegate(IntPtr hMonitor, IntPtr hdcMonitor, ref RECT lprcMonitor, IntPtr dwData);
}
