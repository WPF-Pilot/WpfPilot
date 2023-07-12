namespace WpfPilot.Utility.WpfUtility.Helpers;

using System;
using System.Runtime.CompilerServices;

internal static class ObjectExtensions
{
	private static readonly ConditionalWeakTable<object, RefId> Ids = new();

	public static Guid GetRefId<T>(this T obj)
	{
		if (obj == null)
			return default;

		return Ids.GetOrCreateValue(obj).Id;
	}

	private class RefId
	{
		public Guid Id { get; } = Guid.NewGuid();
	}
}