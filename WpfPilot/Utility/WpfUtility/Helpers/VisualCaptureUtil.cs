// (c) Copyright Cory Plotts.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

namespace WpfPilot.Utility.WpfUtility.Helpers;

using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;

internal static class VisualCaptureUtil
{
	public static VisualBrush? CreateVisualBrushSafe(Visual? visual)
	{
		return IsSafeToVisualize(visual)
			? new VisualBrush(visual)
			: null;
	}

	public static bool IsSafeToVisualize(Visual? visual)
	{
		if (visual is null)
		{
			return false;
		}

		if (visual is Window)
		{
			var source = PresentationSource.FromVisual(visual) as HwndSource;
			return source?.CompositionTarget is not null;
		}

		return true;
	}
}