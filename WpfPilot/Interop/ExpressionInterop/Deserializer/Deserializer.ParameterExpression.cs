namespace Aq.ExpressionJsonSerializer;

using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using Newtonsoft.Json.Linq;
using Expr = System.Linq.Expressions.Expression;

internal partial class Deserializer
{
	private readonly Dictionary<string, ParameterExpression>
		ParameterExpressions = new Dictionary<string, ParameterExpression>();

	private ParameterExpression? ParameterExpression(ExpressionType nodeType, System.Type type, JObject obj)
	{
		var name = Prop(obj, "name", t => t.Value<string>());

		ParameterExpression? result;
		if (ParameterExpressions.TryGetValue(name, out result))
			return result;

		switch (nodeType)
		{
			case ExpressionType.Parameter:
				result = Expr.Parameter(type, name);
				break;
			default:
				throw new NotSupportedException();
		}

		ParameterExpressions[name] = result;
		return result;
	}

	private ParameterExpression? ParameterExpression(JToken token)
	{
		if (token == null || token.Type != JTokenType.Object)
			return null;

		var obj = (JObject) token;
		var nodeType = Prop(obj, "nodeType", Enum<ExpressionType>);
		var type = Prop(obj, "type", Type);
		var typeName = Prop(obj, "typeName", t => t.Value<string>());

		if (typeName != "parameter")
			return null;

		return ParameterExpression(nodeType, type, obj);
	}
}
