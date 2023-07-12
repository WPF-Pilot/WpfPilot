namespace Aq.ExpressionJsonSerializer;

using System;
using System.Linq.Expressions;
using Newtonsoft.Json.Linq;
using Expr = System.Linq.Expressions.Expression;

internal partial class Deserializer
{
	private LambdaExpression LambdaExpression(ExpressionType nodeType, System.Type type, JObject obj)
	{
		var body = Prop(obj, "body", Expression);
		var tailCall = Prop(obj, "tailCall").Value<bool>();
		var parameters = Prop(obj, "parameters", Enumerable(ParameterExpression));

		switch (nodeType)
		{
			case ExpressionType.Lambda:
				return Expr.Lambda(body, tailCall, parameters!);
			default:
				throw new NotSupportedException();
		}
	}

	private LambdaExpression? LambdaExpression(JToken token)
	{
		if (token == null || token.Type != JTokenType.Object)
			return null;

		var obj = (JObject) token;
		var nodeType = Prop(obj, "nodeType", Enum<ExpressionType>);
		var type = Prop(obj, "type", Type);
		var typeName = Prop(obj, "typeName", t => t.Value<string>());

		if (typeName != "lambda")
			return null;

		return LambdaExpression(nodeType, type, obj);
	}
}
