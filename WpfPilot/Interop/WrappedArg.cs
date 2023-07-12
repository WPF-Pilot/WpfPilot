namespace WpfPilot.Interop;

using System;

// This class exists purely to wrap args that are serialized over the wire and unserialized via Newtonsoft.Json, so that type information is preserved.
// Otherwise Newtonsoft.Json seems to convert ints to longs and other unwanted behavior. Note this is necessary even with `TypeNameHandling.All`.
internal sealed class WrappedArg<T>
{
	public static object? Wrap(object? item)
	{
		if (item is null)
			return null;

		Type wrappedType = typeof(WrappedArg<>);
		Type genericType = wrappedType.MakeGenericType(item.GetType());
		var wrappedItem = Activator.CreateInstance(genericType);

		var property = wrappedItem!.GetType().GetProperty("Value");
		property!.SetValue(wrappedItem, item, null);

		return wrappedItem;
	}

	public T? Value { get; set; }
	public string Type { get; } = WrappedArgType;
	public const string WrappedArgType = "p:WrappedArg";
}
