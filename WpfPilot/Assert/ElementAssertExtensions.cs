namespace WpfPilot.Assert;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

internal static class ElementAssertExtensions
{
	public static string GetDiagnosticMessage(Expression body, Exception e)
	{
		var (bodyString, bodyValues) = DebugValueExpressionVisitor.GetDiagnosticString(body);
		return GetDiagnosticMessage(bodyString, bodyValues.Where(x => x.Value is not Element).ToList(), e);
	}

	public static string GetDiagnosticMessage(string expected, IReadOnlyCollection<(string Name, object Value)> actualValues, Exception e)
	{
		var sb = new StringBuilder();
		sb.Append("\nExpected:\n");
		sb.Append('\t');
		sb.Append(expected);

		if (actualValues.Any())
		{
			sb.Append('\n');
			sb.Append('\n');
			sb.Append("Actual:");

			foreach (var (name, value) in actualValues)
			{
				sb.Append('\n');
				sb.Append($"\t{name} == {ToString(value)}");
			}
		}

		if (e != null)
		{
			sb.Append('\n');
			sb.Append('\n');
			sb.Append(e.GetType());
			sb.Append(": ");
			sb.AppendLine(e.Message + "\n");
			sb.Append(e.StackTrace);
		}

		sb.Append('\n');

		return sb.ToString();
	}

	private static string ToString(object? obj)
	{
		while (obj is TargetInvocationException { InnerException: { } } tie)
			obj = tie.InnerException;

		obj = obj is not null and Primitive primitive ? primitive.V : obj;

		switch (obj)
		{
			case null:
				return "null";
			case bool b:
				return b ? "true" : "false";
			case string s:
				return $@"""{s}""";
			case Exception e:
				return $"[{e.GetType()}: {e.Message}]";
		}

		var type = obj.GetType();

		if (type.IsGenericType)
		{
			var genericType = type.GetGenericTypeDefinition();

			if (genericType == typeof(Lazy<>))
				return Property("Value");

			if (genericType == typeof(ValueTuple<>))
				return $"({Property("Item1")})";
			if (genericType == typeof(ValueTuple<,>))
				return $"({Property("Item1")}, {Property("Item2")})";
			if (genericType == typeof(ValueTuple<,,>))
				return $"({Property("Item1")}, {Property("Item2")}, {Property("Item3")})";
			if (genericType == typeof(ValueTuple<,,,>))
				return $"({Property("Item1")}, {Property("Item2")}, {Property("Item3")}, {Property("Item4")})";
			if (genericType == typeof(ValueTuple<,,,,>))
				return $"({Property("Item1")}, {Property("Item2")}, {Property("Item3")}, {Property("Item4")}, {Property("Item5")})";
			if (genericType == typeof(ValueTuple<,,,,,>))
				return $"({Property("Item1")}, {Property("Item2")}, {Property("Item3")}, {Property("Item4")}, {Property("Item5")}, {Property("Item6")})";
			if (genericType == typeof(ValueTuple<,,,,,,>))
				return $"({Property("Item1")}, {Property("Item2")}, {Property("Item3")}, {Property("Item4")}, {Property("Item5")}, {Property("Item6")}, {Property("Item7")})";
			if (genericType == typeof(ValueTuple<,,,,,,,>))
				return $"({Property("Item1")}, {Property("Item2")}, {Property("Item3")}, {Property("Item4")}, {Property("Item5")}, {Property("Item6")}, {Property("Item7")}, {Property("Rest").TrimStart('(')}";

			string Property(string name) => ToString(type.GetProperty(name)!.GetValue(obj));
		}

		var toString = obj.ToString();

		// This means the class doesn't override ToString by itself.
		if (toString == type.ToString())
		{
			try
			{
				return ToPrettyJson(JToken.FromObject(obj), 1).Trim();
			}
			catch (Exception ex)
			{
				return $"<{toString}> [{ex.GetType()}: {ex.Message}]";
			}
		}

		return toString ?? "";
	}

	private static string ToPrettyJson(JToken token, int tabLevel)
	{
		var indent = new string('\t', tabLevel);
		switch (token.Type)
		{
			case JTokenType.Array:
				{
					var children = token.Children();
					if (!children.Any())
						return indent + "[]";

					var childLines = children.Select(c => ToPrettyJson(c, tabLevel + 1));
					var shortJson = '[' + string.Join(", ", childLines.Select(l => l.Trim())) + ']';
					if (shortJson.Length + (tabLevel * 4) < MaxJsonLength)
						return indent + shortJson;

					return indent + "[\n" + string.Join(",\n", childLines) + '\n' + indent + "]";
				}

			case JTokenType.Object:
				{
					var children = token.Children<JProperty>();
					if (!children.Any())
						return indent + "{}";

					var childLines = children.Select(c => ToPrettyJson(c, tabLevel + 1));
					var shortJson = "{ " + string.Join(", ", childLines.Select(l => l.Trim())) + " }";
					if (shortJson.Length + (tabLevel * 4) < MaxJsonLength)
						return indent + shortJson;

					return indent + "{\n" + string.Join(",\n", childLines) + '\n' + indent + "}";
				}

			case JTokenType.Property:
				{
					var prop = token as JProperty;
					return indent + '"' + prop?.Name + "\": " + ToPrettyJson(prop?.Value, tabLevel).Trim();
				}

			default:
				{
					return indent + JsonConvert.SerializeObject(token);
				}
		}
	}

	private const int MaxJsonLength = 160;
}
