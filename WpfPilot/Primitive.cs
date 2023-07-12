namespace WpfPilot;

using System;

/// <summary>
/// Primitive provides more structure than object or JToken, and unlike dynamic, can be used in expressions.<br/>
/// Primitive has built in convenience conversions for most primitives.<br/>
/// However, if you need to convert to a custom type, such as an Enum, you must use <see cref="To{T}"/> to access the value.<br/>
/// <code>
/// ✏️ Primitive p = "abc"; assert(p == "abc", "This assertion is true.");
/// ✏️ Primitive p = 5; assert(p > 4, "This assertion is true.");
/// ✏️ Primitive p = "Hello world"; assert(p.StartsWith("Hello"), "This assertion is true.");
/// </code>
/// </summary>
public readonly struct Primitive : IEquatable<Primitive>, IEquatable<string>, IEquatable<double>, IEquatable<float>, IEquatable<int>, IEquatable<long>, IEquatable<bool>
{
	internal Primitive(object? value)
	{
		V = value;
	}

	public static implicit operator Primitive(string? value) => new(value);
	public static implicit operator Primitive(double? value) => new(value);
	public static implicit operator Primitive(float? value) => new(value);
	public static implicit operator Primitive(int? value) => new(value);
	public static implicit operator Primitive(long? value) => new(value);
	public static implicit operator Primitive(bool? value) => new(value);

	public static implicit operator string?(Primitive value) => (string?) value.V;
	public static implicit operator double?(Primitive value) => (double?) value.V;
	public static implicit operator float?(Primitive value) => (float?) value.V;
	public static implicit operator int?(Primitive value) => (int?) value.V;
	public static implicit operator long?(Primitive value) => (long?) value.V;
	public static implicit operator bool?(Primitive value) => (bool?) value.V;

	public string S => V is null ? "" : (string) V; // Empty string is usually a better default than null when writing element matching functions.
	public T? To<T>()
	{
		return V switch
		{
			null => default,
			_ when typeof(T).IsEnum => (T) Enum.Parse(typeof(T), (string) V),
			_ => (T) V,
		};
	}

	public static bool operator <(Primitive a, Primitive b)
	{
		dynamic a1 = a.V!;
		dynamic b1 = b.V!;
		return a1 < b1;
	}

	public static bool operator >(Primitive a, Primitive b)
	{
		dynamic a1 = a.V!;
		dynamic b1 = b.V!;
		return a1 > b1;
	}

	public static bool operator <=(Primitive a, Primitive b)
	{
		dynamic a1 = a.V!;
		dynamic b1 = b.V!;
		return a1 <= b1;
	}

	public static bool operator >=(Primitive a, Primitive b)
	{
		dynamic a1 = a.V!;
		dynamic b1 = b.V!;
		return a1 >= b1;
	}

	public static bool operator ==(Primitive a, Primitive b)
	{
		dynamic a1 = a.V!;
		dynamic b1 = b.V!;

		if (a1 is string || b1 is string)
			return a1?.ToString() == b1?.ToString();

		return a1 == b1;
	}

	public static bool operator !=(Primitive a, Primitive b)
	{
		dynamic a1 = a.V!;
		dynamic b1 = b.V!;

		if (a1 is string || b1 is string)
			return a1?.ToString() != b1?.ToString();

		return a1 != b1;
	}

	public static Primitive operator +(Primitive a, Primitive b)
	{
		dynamic a1 = a.V!;
		dynamic b1 = b.V!;
		return a1 + b1;
	}

	public static Primitive operator -(Primitive a, Primitive b)
	{
		dynamic a1 = a.V!;
		dynamic b1 = b.V!;
		return a1 - b1;
	}

	public static Primitive operator *(Primitive a, Primitive b)
	{
		dynamic a1 = a.V!;
		dynamic b1 = b.V!;
		return a1 * b1;
	}

	public static Primitive operator /(Primitive a, Primitive b)
	{
		dynamic a1 = a.V!;
		dynamic b1 = b.V!;
		return a1 / b1;
	}

	public static Primitive operator %(Primitive a, Primitive b)
	{
		dynamic a1 = a.V!;
		dynamic b1 = b.V!;
		return a1 % b1;
	}

	public static Primitive operator &(Primitive a, Primitive b)
	{
		dynamic a1 = a.V!;
		dynamic b1 = b.V!;
		return a1 & b1;
	}

	public static Primitive operator |(Primitive a, Primitive b)
	{
		dynamic a1 = a.V!;
		dynamic b1 = b.V!;
		return a1 | b1;
	}

	public static Primitive operator ^(Primitive a, Primitive b)
	{
		dynamic a1 = a.V!;
		dynamic b1 = b.V!;
		return a1 ^ b1;
	}

	public static Primitive operator <<(Primitive a, int b)
	{
		dynamic a1 = a.V!;
		return a1 << b;
	}

	public static Primitive operator >>(Primitive a, int b)
	{
		dynamic a1 = a.V!;
		return a1 >> b;
	}

	public static Primitive operator ~(Primitive a)
	{
		dynamic a1 = a.V!;
		return ~a1;
	}

	public static Primitive operator ++(Primitive a)
	{
		dynamic a1 = a.V!;
		return ++a1;
	}

	public static Primitive operator --(Primitive a)
	{
		dynamic a1 = a.V!;
		return --a1;
	}

	public static Primitive operator +(Primitive a)
	{
		dynamic a1 = a.V!;
		return +a1;
	}

	public static Primitive operator -(Primitive a)
	{
		dynamic a1 = a.V!;
		return -a1;
	}

	public static bool operator true(Primitive a)
	{
		dynamic a1 = a.V!;
		return a1;
	}

	public static bool operator false(Primitive a)
	{
		dynamic a1 = a.V!;
		return !a1;
	}

	public bool Equals(Primitive other)
	{
		dynamic a1 = V!;
		dynamic b1 = other.V!;
		return a1.Equals(b1);
	}

	public override bool Equals(object? obj)
	{
		dynamic a1 = V!;
		dynamic b1 = obj!;
		return a1.Equals(b1);
	}

	public bool Equals(string? other)
	{
		dynamic a1 = V!;
		return a1 == other;
	}

	public bool Equals(double other)
	{
		dynamic a1 = V!;
		return a1 == other;
	}

	public bool Equals(float other)
	{
		dynamic a1 = V!;
		return a1 == other;
	}

	public bool Equals(int other)
	{
		dynamic a1 = V!;
		return a1 == other;
	}

	public bool Equals(long other)
	{
		dynamic a1 = V!;
		return a1 == other;
	}

	public bool Equals(bool other)
	{
		dynamic a1 = V!;
		return a1 == other;
	}

	public override int GetHashCode()
	{
		dynamic a1 = V!;
		return a1.GetHashCode() ?? 0;
	}

	public override string? ToString()
	{
		return V?.ToString();
	}

	internal static readonly Primitive Empty = new(null);

	internal object? V { get; }
}
