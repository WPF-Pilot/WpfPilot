namespace WpfPilot;

using System;
using System.Globalization;
using System.Linq;
using System.Text;

/// <summary>
/// Convenience methods to save a few keystrokes.
/// <code n="samples">
/// ✏️ element["Name"].S.StartsWith("ListItem") -> element["Name"].StartsWith("ListItem")
/// </code>
/// </summary>
public static class PrimitiveExtensions
{
	public static Primitive ToPrimitive(this object obj) => new(obj);
	public static string Join(this Primitive p, string separator, params string[] value) => string.Join(separator, new string[] { p.S! }.Concat(value));
	public static string Join(this Primitive p, string separator, params object[] values) => string.Join(separator, new object[] { p.S! }.Concat(values));
	public static string Join(this Primitive p, string separator, string[] value, int startIndex, int count) => string.Join(separator, new string[] { p.S! }.Concat(value), startIndex, count);
	public static bool IsNullOrEmpty(this Primitive p) => string.IsNullOrEmpty(p.S);
	public static bool IsNullOrWhiteSpace(this Primitive p) => string.IsNullOrWhiteSpace(p.S);
	public static string[] Split(this Primitive p, params char[] separator) => p.S.Split(separator);
	public static string[] Split(this Primitive p, char[] separator, int count) => p.S.Split(separator, count);
	public static string[] Split(this Primitive p, char[] separator, StringSplitOptions options) => p.S.Split(separator, options);
	public static string[] Split(this Primitive p, char[] separator, int count, StringSplitOptions options) => p.S.Split(separator, count, options);
	public static string[] Split(this Primitive p, string[] separator, StringSplitOptions options) => p.S.Split(separator, options);
	public static string[] Split(this Primitive p, string[] separator, int count, StringSplitOptions options) => p.S.Split(separator, count, options);
	public static string Substring(this Primitive p, int startIndex) => p.S.Substring(startIndex);
	public static string Substring(this Primitive p, int startIndex, int length) => p.S.Substring(startIndex, length);
	public static string Trim(this Primitive p, params char[] trimChars) => p.S.Trim(trimChars);
	public static string TrimStart(this Primitive p, params char[] trimChars) => p.S.TrimStart(trimChars);
	public static string TrimEnd(this Primitive p, params char[] trimChars) => p.S.TrimEnd(trimChars);
	public static bool IsNormalized(this Primitive p) => p.S.IsNormalized();
	public static bool IsNormalized(this Primitive p, NormalizationForm normalizationForm) => p.S.IsNormalized(normalizationForm);
	public static string Normalize(this Primitive p) => p.S.Normalize();
	public static string Normalize(this Primitive p, NormalizationForm normalizationForm) => p.S.Normalize(normalizationForm);
	public static bool Contains(this Primitive p, string value) => p.S.Contains(value);
#if NET5_0_OR_GREATER
	public static bool Contains(this Primitive p, string value, StringComparison comparisonType) => p.S.Contains(value, comparisonType);
#endif
	public static bool EndsWith(this Primitive p, string value) => p.S.EndsWith(value);
	public static bool EndsWith(this Primitive p, string value, StringComparison comparisonType) => p.S.EndsWith(value, comparisonType);
	public static bool EndsWith(this Primitive p, string value, bool ignoreCase, CultureInfo culture) => p.S.EndsWith(value, ignoreCase, culture);
	public static int IndexOf(this Primitive p, char value) => p.S.IndexOf(value);
	public static int IndexOf(this Primitive p, char value, int startIndex) => p.S.IndexOf(value, startIndex);
	public static int IndexOf(this Primitive p, char value, int startIndex, int count) => p.S.IndexOf(value, startIndex, count);
	public static int IndexOfAny(this Primitive p, char[] anyOf) => p.S.IndexOfAny(anyOf);
	public static int IndexOfAny(this Primitive p, char[] anyOf, int startIndex) => p.S.IndexOfAny(anyOf, startIndex);
	public static int IndexOfAny(this Primitive p, char[] anyOf, int startIndex, int count) => p.S.IndexOfAny(anyOf, startIndex, count);
	public static int IndexOf(this Primitive p, string value) => p.S.IndexOf(value);
	public static int IndexOf(this Primitive p, string value, int startIndex) => p.S.IndexOf(value, startIndex);
	public static int IndexOf(this Primitive p, string value, int startIndex, int count) => p.S.IndexOf(value, startIndex, count);
	public static int IndexOf(this Primitive p, string value, StringComparison comparisonType) => p.S.IndexOf(value, comparisonType);
	public static int IndexOf(this Primitive p, string value, int startIndex, StringComparison comparisonType) => p.S.IndexOf(value, startIndex, comparisonType);
	public static int IndexOf(this Primitive p, string value, int startIndex, int count, StringComparison comparisonType) => p.S.IndexOf(value, startIndex, count, comparisonType);
	public static int LastIndexOf(this Primitive p, char value) => p.S.LastIndexOf(value);
	public static int LastIndexOf(this Primitive p, char value, int startIndex) => p.S.LastIndexOf(value, startIndex);
	public static int LastIndexOf(this Primitive p, char value, int startIndex, int count) => p.S.LastIndexOf(value, startIndex, count);
	public static int LastIndexOfAny(this Primitive p, char[] anyOf) => p.S.LastIndexOfAny(anyOf);
	public static int LastIndexOfAny(this Primitive p, char[] anyOf, int startIndex) => p.S.LastIndexOfAny(anyOf, startIndex);
	public static int LastIndexOfAny(this Primitive p, char[] anyOf, int startIndex, int count) => p.S.LastIndexOfAny(anyOf, startIndex, count);
	public static int LastIndexOf(this Primitive p, string value) => p.S.LastIndexOf(value);
	public static int LastIndexOf(this Primitive p, string value, int startIndex) => p.S.LastIndexOf(value, startIndex);
	public static int LastIndexOf(this Primitive p, string value, int startIndex, int count) => p.S.LastIndexOf(value, startIndex, count);
	public static int LastIndexOf(this Primitive p, string value, StringComparison comparisonType) => p.S.LastIndexOf(value, comparisonType);
	public static int LastIndexOf(this Primitive p, string value, int startIndex, StringComparison comparisonType) => p.S.LastIndexOf(value, startIndex, comparisonType);
	public static int LastIndexOf(this Primitive p, string value, int startIndex, int count, StringComparison comparisonType) => p.S.LastIndexOf(value, startIndex, count, comparisonType);
	public static string PadLeft(this Primitive p, int totalWidth) => p.S.PadLeft(totalWidth);
	public static string PadLeft(this Primitive p, int totalWidth, char paddingChar) => p.S.PadLeft(totalWidth, paddingChar);
	public static string PadRight(this Primitive p, int totalWidth) => p.S.PadRight(totalWidth);
	public static string PadRight(this Primitive p, int totalWidth, char paddingChar) => p.S.PadRight(totalWidth, paddingChar);
	public static bool StartsWith(this Primitive p, string value) => p.S.StartsWith(value);
	public static bool StartsWith(this Primitive p, string value, StringComparison comparisonType) => p.S.StartsWith(value, comparisonType);
	public static bool StartsWith(this Primitive p, string value, bool ignoreCase, CultureInfo culture) => p.S.StartsWith(value, ignoreCase, culture);
	public static string ToLower(this Primitive p) => p.S.ToLower();
	public static string ToLower(this Primitive p, CultureInfo culture) => p.S.ToLower(culture);
	public static string ToLowerInvariant(this Primitive p) => p.S.ToLowerInvariant();
	public static string ToUpper(this Primitive p) => p.S.ToUpper();
	public static string ToUpper(this Primitive p, CultureInfo culture) => p.S.ToUpper(culture);
	public static string ToUpperInvariant(this Primitive p) => p.S.ToUpperInvariant();
	public static string Trim(this Primitive p) => p.S.Trim();
	public static string Insert(this Primitive p, int startIndex, string value) => p.S.Insert(startIndex, value);
	public static string Replace(this Primitive p, string oldValue, string newValue) => p.S.Replace(oldValue, newValue);
	public static string Remove(this Primitive p, int startIndex, int count) => p.S.Remove(startIndex, count);
	public static string Remove(this Primitive p, int startIndex) => p.S.Remove(startIndex);
	public static string Format(this Primitive p, params object[] args) => string.Format(p.S, args);
	public static string Concat(this Primitive p, params object[] args) => string.Concat(new object[] { p.S }.Concat(args));
	public static CharEnumerator GetEnumerator(this Primitive p) => p.S.GetEnumerator();
}
