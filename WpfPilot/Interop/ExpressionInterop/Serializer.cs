#pragma warning disable SA1501 // Statement should not be on a single line

namespace Aq.ExpressionJsonSerializer;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Newtonsoft.Json;
using WpfPilot.Interop;

internal sealed partial class Serializer
{
	public static void Serialize(
		JsonWriter writer,
		JsonSerializer serializer,
		Expression expression)
	{
		var s = new Serializer(writer, serializer);
		s.ExpressionInternal(expression);
	}

	private readonly JsonWriter Writer;
	private readonly JsonSerializer ExpSerializer;

	private Serializer(JsonWriter writer, JsonSerializer serializer)
	{
		Writer = writer;
		ExpSerializer = serializer;
	}

	private Action Serialize(object value, System.Type type)
	{
		return () => ExpSerializer.Serialize(Writer, value, type);
	}

	private void Prop(string name, bool value)
	{
		Writer.WritePropertyName(name);
		Writer.WriteValue(value);
	}

	private void Prop(string name, int value)
	{
		Writer.WritePropertyName(name);
		Writer.WriteValue(value);
	}

	private void Prop(string name, string value)
	{
		Writer.WritePropertyName(name);
		Writer.WriteValue(value);
	}

	private void Prop(string name, Action valueWriter)
	{
		Writer.WritePropertyName(name);
		valueWriter();
	}

	private Action Enum<TEnum>(TEnum value)
	{
		return () => EnumInternal(value);
	}

	private void EnumInternal<TEnum>(TEnum value)
	{
		Writer.WriteValue(System.Enum.GetName(typeof(TEnum), value));
	}

	private Action Enumerable<T>(IEnumerable<T> items, Func<T, Action> func)
	{
		return () => EnumerableInternal(items, func);
	}

	private void EnumerableInternal<T>(IEnumerable<T> items, Func<T, Action> func)
	{
		if (items == null)
		{
			Writer.WriteNull();
		}
		else
		{
			Writer.WriteStartArray();
			foreach (var item in items)
			{
				func(item)();
			}

			Writer.WriteEndArray();
		}
	}

	private Action Expression(Expression expression)
	{
		return () => ExpressionInternal(expression);
	}

	private void ExpressionInternal(Expression expression)
	{
		if (expression == null)
		{
			Writer.WriteNull();
			return;
		}

		while (expression.CanReduce)
		{
			expression = expression.Reduce();
		}

		Writer.WriteStartObject();

		if (expression is MemberExpression memberExpression && memberExpression.Member.DeclaringType!.Name.Contains("<>c__"))
		{
			var isLocalVariable = memberExpression.Member.DeclaringType!.Name.Contains("<>c__");
			if (isLocalVariable && TryGetConstantValueFromMemberExpression(memberExpression, out var constantValue, out var constantValueType) && IsSerializable(constantValue))
			{
				var constExpr = System.Linq.Expressions.Expression.Constant(constantValue, constantValueType);
				expression = constExpr;
			}
			else
			{
				// Eg `User userArg`
				var info = memberExpression.Member.ToString()?.Split(' ') ?? new[] { "Unknown", "unknown" };

				var typeName = info.FirstOrDefault() ?? "Unknown";
				var variableName = info.LastOrDefault() ?? "unknown";

				throw new InvalidOperationException(
					$"Local variable of type `{typeName}` with name `{variableName}` cannot be serialized over the wire.\n" +
					"Make this type serializable or inline the variable.\n" +
					"Inlined `() => new MyUnserializableType(1, 2, 3);`\n" +
					"Variable `() => myUnserializableVariable;`");
			}
		}

		Prop("nodeType", Enum(expression.NodeType));
		Prop("type", Type(expression.Type));

		if (BinaryExpression(expression)) { goto end; }
		if (BlockExpression(expression)) { goto end; }
		if (ConditionalExpression(expression)) { goto end; }
		if (ConstantExpression(expression)) { goto end; }
		if (DebugInfoExpression(expression)) { goto end; }
		if (DefaultExpression(expression)) { goto end; }
		if (DynamicExpression(expression)) { goto end; }
		if (GotoExpression(expression)) { goto end; }
		if (IndexExpression(expression)) { goto end; }
		if (InvocationExpression(expression)) { goto end; }
		if (LabelExpression(expression)) { goto end; }
		if (LambdaExpression(expression)) { goto end; }
		if (ListInitExpression(expression)) { goto end; }
		if (LoopExpression(expression)) { goto end; }
		if (MemberExpression(expression)) { goto end; }
		if (MemberInitExpression(expression)) { goto end; }
		if (MethodCallExpression(expression)) { goto end; }
		if (NewArrayExpression(expression)) { goto end; }
		if (NewExpression(expression)) { goto end; }
		if (ParameterExpression(expression)) { goto end; }
		if (RuntimeVariablesExpression(expression)) { goto end; }
		if (SwitchExpression(expression)) { goto end; }
		if (TryExpression(expression)) { goto end; }
		if (TypeBinaryExpression(expression)) { goto end; }
		if (UnaryExpression(expression)) { goto end; }

		throw new NotSupportedException();

	end:
		Writer.WriteEndObject();
	}

	public static bool IsSerializable(object? result)
	{
		try
		{
			var serialized = JsonConvert.SerializeObject(result, Instance);
			_ = JsonConvert.DeserializeObject(serialized, Instance);
			return true;
		}
		catch
		{
			return false;
		}
	}

	private static JsonSerializerSettings Instance { get; } = new JsonSerializerSettings
	{
		ContractResolver = new RobustContractResolver(),
		ConstructorHandling = ConstructorHandling.AllowNonPublicDefaultConstructor,

		// Include type information so we can deserialize to the correct type. Otherwise we could get a long when we wanted an int, etc.
		TypeNameHandling = TypeNameHandling.All,

		// Some apps have a deep object graph.
		MaxDepth = 1000,
	};
}
