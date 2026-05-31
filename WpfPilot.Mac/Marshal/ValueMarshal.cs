namespace WpfPilot.Mac;

using System;
using WpfPilot.Interop;

/// <summary>
/// Wraps/unwraps argument and result values so primitive types survive the JSON transport. Shared by the
/// test-side driver (which wraps arguments and unwraps results) and the app-side server (which unwraps
/// arguments and wraps results), so it lives in the common assembly both sides load.
/// </summary>
/// <remarks>
/// Newtonsoft widens <see cref="int"/> to <see cref="long"/> on a round-trip even with
/// <c>TypeNameHandling.All</c> unless the value is wrapped in a typed envelope; <c>WrappedArg&lt;T&gt;</c>
/// preserves the original CLR type.
/// </remarks>
public static class ValueMarshal
{
	public static object? Wrap(object? value) => WrappedArg<object>.Wrap(value);

	public static object? Unwrap(object? wrapped)
	{
		if (wrapped is null)
			return null;

		var type = wrapped.GetType();
		if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(WrappedArg<>))
			return type.GetProperty("Value")!.GetValue(wrapped);

		return wrapped;
	}
}
