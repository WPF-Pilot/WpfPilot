namespace WpfPilot.Utility.WpfUtility.Helpers;

using System.Windows;

internal static class ResourceKeyHelper
{
	public static bool IsValidResourceKey(object? key)
	{
		return key is not null
			   && ReferenceEquals(key, DependencyProperty.UnsetValue) == false;
	}
}