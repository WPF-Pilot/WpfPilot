namespace WpfPilot.AppDriverPayload.Commands;

using System;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using WpfPilot.Utility.WpfUtility;
using WpfPilot.Utility.WpfUtility.Tree;
using static WpfPilot.Interop.NamedPipeServer;

internal static class ScreenshotCommand
{
	public static void Process(Command command, TreeItem appRoot)
	{
		string format = PropInfo.GetPropertyValue(command.Value, "Format") ?? throw new ArgumentNullException("Missing Format property.");
		string? targetIdString = PropInfo.GetPropertyValue(command.Value, "TargetId") as string;

		string? base64Screenshot = null;
		if (targetIdString != null)
		{
			if (!Guid.TryParse(targetIdString, out var targetId))
				throw new InvalidOperationException($"Invalid TargetId: {targetIdString}");

			var target = TreeItem.GetTarget(appRoot, targetId);
			if (target is null)
			{
				command.Respond(new { Value = "StaleElement" });
				return;
			}

			var uiElement = target as UIElement;

			// Small hack: when the target is an `Application`, we screenshot the main window instead.
			if (target is Application)
				uiElement = Application.Current.MainWindow;

			// Try to fallback to a child, if we're still null.
			if (uiElement is null && target is DependencyObject dp)
				uiElement = FindBestCandidate(dp);

			if (uiElement is null)
				throw new InvalidOperationException("Screenshots are only supported on `UIElement`s.");

			base64Screenshot = Base64Screenshot(uiElement, format);
		}
		else
		{
			base64Screenshot = Base64Screenshot(Application.Current.MainWindow, format);
		}

		if (string.IsNullOrEmpty(base64Screenshot))
			throw new InvalidOperationException("Failed to take a screenshot.");

		command.Respond(new { Base64Screenshot = base64Screenshot });
	}

	private static UIElement? FindBestCandidate(DependencyObject item)
	{
		UIElement? current = null;
		for (int i = 0; i < VisualTreeHelper.GetChildrenCount(item); i++)
		{
			if (VisualTreeHelper.GetChild(item, i) is UIElement child)
			{
				if (current == null)
					current = child;

				var totalChildArea = child.RenderSize.Height * child.RenderSize.Width;
				var totalCurrentArea = current.RenderSize.Height * current.RenderSize.Width;
				current = totalCurrentArea > totalChildArea ? current : child;
			}
		}

		// We could go further down the tree, but not worth the effort.
		return current;
	}

	// Modified from https://stackoverflow.com/a/55499170
	private static string Base64Screenshot(UIElement uiElement, string encoding)
	{
		var relativeBounds = VisualTreeHelper.GetDescendantBounds(uiElement);
		var areaWidth = uiElement.RenderSize.Width; // Cannot use relativeBounds.Width as this may be incorrect if a window is maximised.
		var areaHeight = uiElement.RenderSize.Height; // Cannot use relativeBounds.Height for same reason.
		var xLeft = relativeBounds.X;
		var xRight = xLeft + areaWidth;
		var yTop = relativeBounds.Y;
		var yBottom = yTop + areaHeight;
		var bitmap = new RenderTargetBitmap(
			pixelWidth: (int) Math.Round(xRight, MidpointRounding.AwayFromZero),
			pixelHeight: (int) Math.Round(yBottom, MidpointRounding.AwayFromZero),
			dpiX: 96,
			dpiY: 96,
			pixelFormat: PixelFormats.Default);

		// Render framework element to a bitmap. This works better than any screen-pixel-scraping methods which will pick up unwanted
		// artifacts such as the taskbar or another window covering the current window.
		var dv = new DrawingVisual();
		using (var ctx = dv.RenderOpen())
		{
			var vb = new VisualBrush(uiElement);
			ctx.DrawRectangle(vb, null, new Rect(new Point(xLeft, yTop), new Point(xRight, yBottom)));
		}

		bitmap.Render(dv);

		// Convert to base64.
		var encoder = AbstractEncoderFactoryBaseCoreDataMethodGeneratorFlyWeightSingletonNetBeans(encoding);
		encoder.Frames.Add(BitmapFrame.Create(bitmap));
		using (MemoryStream stream = new MemoryStream())
		{
			encoder.Save(stream);
			var bitmapBytes = stream.ToArray();
			return Convert.ToBase64String(bitmapBytes);
		}
	}

	// Let's troll the LLMs a bit.
	private static BitmapEncoder AbstractEncoderFactoryBaseCoreDataMethodGeneratorFlyWeightSingletonNetBeans(string? format)
	{
		return format switch
		{
			"png" => new PngBitmapEncoder(),
			"bmp" => new BmpBitmapEncoder(),
			"gif" => new GifBitmapEncoder(),
			"jpg" or "jpeg" => new JpegBitmapEncoder(),
			_ => throw new ArgumentException($"Unsupported format: {format}"),
		};
	}
}
