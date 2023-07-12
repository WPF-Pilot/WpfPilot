namespace WpfPilot.Utility.WpfUtility.SelectionHighlight;

using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;

internal class SelectionAdorner : Adorner, IDisposable
{
	static SelectionAdorner()
	{
		IsHitTestVisibleProperty.OverrideMetadata(typeof(SelectionAdorner), new UIPropertyMetadata(false));
		UseLayoutRoundingProperty.OverrideMetadata(typeof(SelectionAdorner), new FrameworkPropertyMetadata(true));
	}

	public SelectionAdorner(UIElement adornedElement)
		: base(adornedElement)
	{
		SelectionHighlightOptions.Default.PropertyChanged += this.SelectionHighlightOptionsOnPropertyChanged;
	}

	public AdornerLayer? AdornerLayer { get; set; }

	protected override void OnRender(DrawingContext drawingContext)
	{
		if (SelectionHighlightOptions.Default.HighlightSelectedItem == false)
		{
			return;
		}

		if (AreClose(this.ActualWidth, 0)
			|| AreClose(this.ActualHeight, 0))
		{
			return;
		}

		var pen = SelectionHighlightOptions.Default.Pen;

		drawingContext.DrawRectangle(SelectionHighlightOptions.Default.Background, pen, new Rect(0, 0, this.ActualWidth, this.ActualHeight));
	}

	public void Dispose()
	{
		SelectionHighlightOptions.Default.PropertyChanged -= this.SelectionHighlightOptionsOnPropertyChanged;

		this.AdornerLayer?.Remove(this);
	}

	private void SelectionHighlightOptionsOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
	{
		this.InvalidateVisual();
	}

	private static bool AreClose(double value1, double value2)
	{
		// in case they are Infinities (then epsilon check does not work)
		// ReSharper disable once CompareOfFloatsByEqualityOperator
		if (value1 == value2)
		{
			return true;
		}

		// This computes (|value1-value2| / (|value1| + |value2| + 10.0)) &lt; DBL_EPSILON
		var eps = (Math.Abs(value1) + Math.Abs(value2) + 10.0) * 2.2204460492503131e-016;
		var delta = value1 - value2;
		return -eps < delta && eps > delta;
	}
}