namespace WpfPilot.Utility.WpfUtility.SelectionHighlight;

using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using WpfPilot.Utility.WpfUtility.Helpers;

internal static class UIHighlight
{
	// TODO: Change this to a void method.
	public static void Select(DependencyObject dependencyObject)
	{
		try
		{
			// Clear previous selection.
			CurrentHighlight?.Dispose();

			var uiElement = FindUIElement(dependencyObject);
			if (uiElement is null)
				return;

			var highlight = CreateAndAttachSelectionHighlightAdorner(uiElement);
			CurrentHighlight = highlight;
		}
		catch
		{
			// This method is a "best attempt". If it fails, it's not a big deal.
		}
	}

	private static IDisposable? CreateAndAttachSelectionHighlightAdorner(UIElement uiElement)
	{
		var adornerLayer = AdornerLayer.GetAdornerLayer(uiElement);
		if (adornerLayer is null)
			return null;

		var selectionAdorner = CreateSelectionAdorner(uiElement, adornerLayer);

		adornerLayer.Add(selectionAdorner);
		return selectionAdorner;
	}

	private static UIElement? FindUIElement(DependencyObject dependencyObject)
	{
		return dependencyObject switch
		{
			UIElement uiElement => uiElement,
			ColumnDefinition columnDefinition => columnDefinition.Parent as UIElement,
			RowDefinition rowDefinition => rowDefinition.Parent as UIElement,
			ContentElement contentElement => contentElement.GetUIParent() as UIElement ?? contentElement.GetParent() as UIElement,
			_ => null
		};
	}

	private static SelectionAdorner CreateSelectionAdorner(UIElement uiElement, AdornerLayer? adornerLayer)
	{
		return new SelectionAdorner(uiElement)
		{
			AdornerLayer = adornerLayer
		};
	}

	private static IDisposable? CurrentHighlight { get; set; } = null;
}