﻿namespace WpfPilot.Utility.WpfUtility.Converters;

using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using WpfPilot.Utility.WpfUtility.Helpers;

[ValueConversion(typeof(object), typeof(object))]
[ValueConversion(typeof(object), typeof(Style))]
internal class NullStyleConverter : IValueConverter
{
	public static readonly NullStyleConverter DefaultInstance = new();

	public object? Convert(object? value, Type? targetType, object? parameter, CultureInfo? culture)
	{
		if (value is not null)
		{
			return value;
		}

		return parameter switch
		{
			FrameworkElement fe => FrameworkElementHelper.GetStyle(fe),
			FrameworkContentElement fce => FrameworkElementHelper.GetStyle(fce),
			_ => null
		};
	}

	public object ConvertBack(object? value, Type? targetType, object? parameter, CultureInfo? culture)
	{
		return Binding.DoNothing;
	}
}