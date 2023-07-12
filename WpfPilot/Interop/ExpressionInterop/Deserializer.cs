#pragma warning disable SA1025

namespace Aq.ExpressionJsonSerializer;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Newtonsoft.Json.Linq;

internal sealed partial class Deserializer
{
	public static Expression Deserialize(Assembly assembly, JToken token)
	{
		var d = new Deserializer(assembly);
		return d.Expression(token)!;
	}

	private readonly Dictionary<string, LabelTarget> LabelTargets = new Dictionary<string, LabelTarget>();
	private readonly Assembly Assembly;

	private Deserializer(Assembly assembly)
	{
		Assembly = assembly;
	}

	private object Deserialize(JToken token, System.Type type)
	{
		return token.ToObject(type)!;
	}

	private T Prop<T>(JObject obj, string name, Func<JToken, T>? result = null)
	{
		var prop = obj.Property(name);

		if (result == null)
			result = token => (token != null ? token.Value<T>() : default)!;

		return result(prop == null ? null : prop.Value);
	}

	private JToken Prop(JObject obj, string name)
	{
		return obj.Property(name)!.Value;
	}

	private T Enum<T>(JToken token)
	{
		return (T) System.Enum.Parse(typeof(T), token.Value<string>());
	}

	private Func<JToken, IEnumerable<T>?> Enumerable<T>(Func<JToken, T> result)
	{
		return token =>
		{
			if (token == null || token.Type != JTokenType.Array)
				return null;

			var array = (JArray) token;
			return array.Select(result);
		};
	}

	private Expression? Expression(JToken token)
	{
		if (token == null || token.Type != JTokenType.Object)
			return null;

		var obj = (JObject) token;
		var nodeType = Prop(obj, "nodeType", Enum<ExpressionType>);
		var type = Prop(obj, "type", Type);
		var typeName = Prop(obj, "typeName", t => t.Value<string>());

		switch (typeName)
		{
			case "binary":              return BinaryExpression(nodeType, type, obj);
			case "block":               return BlockExpression(nodeType, type, obj);
			case "conditional":         return ConditionalExpression(nodeType, type, obj);
			case "constant":            return ConstantExpression(nodeType, type, obj);
			case "debugInfo":           return DebugInfoExpression(nodeType, type, obj);
			case "default":             return DefaultExpression(nodeType, type, obj);
			case "dynamic":             return DynamicExpression(nodeType, type, obj);
			case "goto":                return GotoExpression(nodeType, type, obj);
			case "index":               return IndexExpression(nodeType, type, obj);
			case "invocation":          return InvocationExpression(nodeType, type, obj);
			case "label":               return LabelExpression(nodeType, type, obj);
			case "lambda":              return LambdaExpression(nodeType, type, obj);
			case "listInit":            return ListInitExpression(nodeType, type, obj);
			case "loop":                return LoopExpression(nodeType, type, obj);
			case "member":              return MemberExpression(nodeType, type, obj);
			case "memberInit":          return MemberInitExpression(nodeType, type, obj);
			case "methodCall":          return MethodCallExpression(nodeType, type, obj);
			case "newArray":            return NewArrayExpression(nodeType, type, obj);
			case "new":                 return NewExpression(nodeType, type, obj);
			case "parameter":           return ParameterExpression(nodeType, type, obj);
			case "runtimeVariables":    return RuntimeVariablesExpression(nodeType, type, obj);
			case "switch":              return SwitchExpression(nodeType, type, obj);
			case "try":                 return TryExpression(nodeType, type, obj);
			case "typeBinary":          return TypeBinaryExpression(nodeType, type, obj);
			case "unary":               return UnaryExpression(nodeType, type, obj);
		}

		throw new NotSupportedException();
	}

	private LabelTarget CreateLabelTarget(string name, Type type)
	{
		if (LabelTargets.ContainsKey(name))
			return LabelTargets[name];

		LabelTargets[name] = System.Linq.Expressions.Expression.Label(type, name);

		return LabelTargets[name];
	}
}
