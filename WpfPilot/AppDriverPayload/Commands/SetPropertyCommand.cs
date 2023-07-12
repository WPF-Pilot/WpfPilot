namespace WpfPilot.AppDriverPayload.Commands;

using System;
using System.Globalization;
using System.Reflection;
using System.Windows;
using System.Windows.Media;
using WpfPilot.Interop;
using WpfPilot.Utility.WpfUtility;
using WpfPilot.Utility.WpfUtility.SelectionHighlight;
using WpfPilot.Utility.WpfUtility.Tree;
using static WpfPilot.Interop.NamedPipeServer;

internal static class SetPropertyCommand
{
	public static void Process(Command command, TreeItem appRoot)
	{
		var targetIdString = PropInfo.GetPropertyValue(command.Value, "TargetId") ?? throw new ArgumentNullException("Missing TargetId property.");
		if (!Guid.TryParse(targetIdString, out Guid targetId))
			throw new ArgumentException($"Invalid TargetId `{targetIdString}`");

		var arg = PropInfo.GetPropertyValue(command.Value, "PropertyValue") ?? throw new ArgumentNullException($"Missing PropertyValue property.");
		var propertyName = PropInfo.GetPropertyValue(command.Value, "PropertyName") ?? throw new ArgumentNullException($"Missing PropertyName property.");
		var target = TreeItem.GetTarget(appRoot, targetId) ?? throw new InvalidOperationException("Stale element. Cannot find the target element in the Visual Tree.");

		// Deserialize the property.
		arg = ArgsMapper.MapSingle(arg);
		if (arg is Delegate) // Invoke unwrapped `Eval` function.
			arg = arg.DynamicInvoke(target);

		// Invoke the property.
		PropertyInfo? property = target.GetType().GetProperty(propertyName);
		if (property == null)
			throw new InvalidOperationException($"Cannot find property `{propertyName}` on element type `{target.GetType()}`.");

		// `Primitive` may send strings that are convertible to the target property type.
		// We do that conversion here.
		if (property.PropertyType != typeof(string) && arg is string s)
			arg = ConvertFromString(property.PropertyType, s);

		if (target is DependencyObject dp)
			UIHighlight.Select(dp);

		property.SetValue(target, arg, null);

		command.Respond(new { Success = true });
	}

	private static object? ConvertFromString(Type targetType, string valueString)
	{
		return targetType switch
		{
			_ when targetType.IsEnum => Enum.Parse(targetType, valueString),
			_ when targetType == typeof(SolidColorBrush) => new SolidColorBrush((Color) ColorConverter.ConvertFromString(valueString)),
			_ when targetType == typeof(FontFamily) => new FontFamily(valueString),
			_ when targetType == typeof(Size) => Size.Parse(valueString),
			_ when targetType == typeof(Point) => Point.Parse(valueString),
			_ when targetType == typeof(Thickness) => ThicknessFromString(valueString),
			_ when targetType == typeof(Rect) => Rect.Parse(valueString),
			_ when targetType == typeof(FontWeight) => FontWeightStringToKnownWeight(valueString),
			_ when targetType == typeof(FontStyle) => ConvertToFontStyle(valueString),
			_ when targetType == typeof(FontStretch) => FontStretchStringToKnownStretch(valueString),
			_ => valueString,
		};
	}

	// Modified from WPF `FontWeights`.
	private static FontWeight FontWeightStringToKnownWeight(string s)
	{
		switch (s.Length)
		{
			case 4:
				if (s.Equals("Bold", StringComparison.OrdinalIgnoreCase))
					return FontWeights.Bold;

				if (s.Equals("Thin", StringComparison.OrdinalIgnoreCase))
					return FontWeights.Thin;
				break;
			case 5:
				if (s.Equals("Black", StringComparison.OrdinalIgnoreCase))
					return FontWeights.Black;

				if (s.Equals("Light", StringComparison.OrdinalIgnoreCase))
					return FontWeights.Light;

				if (s.Equals("Heavy", StringComparison.OrdinalIgnoreCase))
					return FontWeights.Heavy;
				break;
			case 6:
				if (s.Equals("Normal", StringComparison.OrdinalIgnoreCase))
					return FontWeights.Normal;

				if (s.Equals("Medium", StringComparison.OrdinalIgnoreCase))
					return FontWeights.Medium;
				break;
			case 7:
				if (s.Equals("Regular", StringComparison.OrdinalIgnoreCase))
					return FontWeights.Regular;
				break;
			case 8:
				if (s.Equals("SemiBold", StringComparison.OrdinalIgnoreCase))
					return FontWeights.SemiBold;

				if (s.Equals("DemiBold", StringComparison.OrdinalIgnoreCase))
					return FontWeights.DemiBold;
				break;
			case 9:
				if (s.Equals("ExtraBold", StringComparison.OrdinalIgnoreCase))
					return FontWeights.ExtraBold;

				if (s.Equals("UltraBold", StringComparison.OrdinalIgnoreCase))
					return FontWeights.UltraBold;
				break;
			case 10:
				if (s.Equals("ExtraLight", StringComparison.OrdinalIgnoreCase))
					return FontWeights.ExtraLight;

				if (s.Equals("UltraLight", StringComparison.OrdinalIgnoreCase))
					return FontWeights.UltraLight;

				if (s.Equals("ExtraBlack", StringComparison.OrdinalIgnoreCase))
					return FontWeights.ExtraBlack;

				if (s.Equals("UltraBlack", StringComparison.OrdinalIgnoreCase))
					return FontWeights.UltraBlack;
				break;
		}

		if (int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out var result))
			return FontWeight.FromOpenTypeWeight(result);

		throw new ArgumentOutOfRangeException($"Could not convert `{s}` to a FontWeight");
	}

	// Modified from WPF `FontStyles`.
	private static FontStyle ConvertToFontStyle(string value)
	{
		if (value.ToLowerInvariant() == "normal")
			return FontStyles.Normal;
		else if (value.ToLowerInvariant() == "italic")
			return FontStyles.Italic;
		else if (value.ToLowerInvariant() == "oblique")
			return FontStyles.Oblique;

		throw new ArgumentOutOfRangeException($"Could not convert `{value}` to a FontStyle");
	}

	// Modified from WPF `FontStretches`.
	private static FontStretch FontStretchStringToKnownStretch(string s)
	{
		switch (s.Length)
		{
			case 6:
				if (s.Equals("Normal", StringComparison.OrdinalIgnoreCase))
					return FontStretches.Normal;
				if (s.Equals("Medium", StringComparison.OrdinalIgnoreCase))
					return FontStretches.Medium;
				break;
			case 8:
				if (s.Equals("Expanded", StringComparison.OrdinalIgnoreCase))
					return FontStretches.Expanded;
				break;
			case 9:
				if (s.Equals("Condensed", StringComparison.OrdinalIgnoreCase))
					return FontStretches.Condensed;
				break;
			case 12:
				if (s.Equals("SemiExpanded", StringComparison.OrdinalIgnoreCase))
					return FontStretches.SemiExpanded;
				break;
			case 13:
				if (s.Equals("SemiCondensed", StringComparison.OrdinalIgnoreCase))
					return FontStretches.SemiCondensed;
				if (s.Equals("ExtraExpanded", StringComparison.OrdinalIgnoreCase))
					return FontStretches.ExtraExpanded;
				if (s.Equals("UltraExpanded", StringComparison.OrdinalIgnoreCase))
					return FontStretches.UltraExpanded;
				break;
			case 14:
				if (s.Equals("UltraCondensed", StringComparison.OrdinalIgnoreCase))
					return FontStretches.UltraCondensed;
				if (s.Equals("ExtraCondensed", StringComparison.OrdinalIgnoreCase))
					return FontStretches.ExtraCondensed;
				break;
		}

		if (int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out var result))
			return FontStretch.FromOpenTypeStretch(result);

		throw new ArgumentOutOfRangeException($"Could not convert `{s}` to a FontStretch");
	}

	private static Thickness ThicknessFromString(string s)
	{
		ThicknessConverter myThicknessConverter = new();
		return (Thickness) myThicknessConverter.ConvertFromString(null, CultureInfo.InvariantCulture, s)!;
	}
}
