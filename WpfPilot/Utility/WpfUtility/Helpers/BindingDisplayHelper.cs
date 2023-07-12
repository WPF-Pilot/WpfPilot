﻿namespace WpfPilot.Utility.WpfUtility.Helpers;

using System.Collections.Generic;
using System.Text;
using System.Windows.Data;

internal static class BindingDisplayHelper
{
	/// <summary>
	/// Build up a string describing the Binding. Path, ElementName and RelativeSource (if present).
	/// </summary>
	public static string BuildBindingDescriptiveString(BindingBase binding)
	{
		return BuildBindingDescriptiveString(binding, "Path", "ElementName", "RelativeSource");
	}

	/// <summary>
	/// Build up a string describing the Binding.
	/// </summary>
	public static string BuildBindingDescriptiveString(BindingBase binding, params string[] propertyNames)
	{
		if (binding is MultiBinding multiBinding)
		{
			return BuildMultiBindingDescriptiveString(multiBinding.Bindings, propertyNames);
		}

		if (binding is PriorityBinding priorityBinding)
		{
			return BuildMultiBindingDescriptiveString(priorityBinding.Bindings, propertyNames);
		}

		var propertyValues = new List<string>(propertyNames.Length);

		var xaml = XamlWriterHelper.GetXamlAsXElement(binding).RemoveNamespaces();

		foreach (var propertyName in propertyNames)
		{
			var attribute = xaml.Attribute(propertyName);

			if (attribute is not null)
			{
				propertyValues.Add($"{propertyName}={attribute.Value}");
			}
		}

		return string.Join(", ", propertyValues);
	}

	/// <summary>
	/// Build up a string of Paths for a MultiBinding separated by ;
	/// </summary>
	private static string BuildMultiBindingDescriptiveString(ICollection<BindingBase> bindings, params string[] propertyNames)
	{
		var first = true;
		var sb = new StringBuilder(" {Paths=");
		foreach (var binding in bindings)
		{
			if (first == false)
			{
				sb.Append(';');
			}

			sb.Append(BuildBindingDescriptiveString(binding, propertyNames));
			first = false;
		}

		sb.Append('}');

		return sb.ToString();
	}
}