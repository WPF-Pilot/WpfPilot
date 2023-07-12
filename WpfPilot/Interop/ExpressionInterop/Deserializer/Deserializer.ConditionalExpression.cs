namespace Aq.ExpressionJsonSerializer;

using System;
using System.Linq.Expressions;
using Newtonsoft.Json.Linq;
using Expr = System.Linq.Expressions.Expression;

internal partial class Deserializer
{
	private ConditionalExpression ConditionalExpression(ExpressionType nodeType, System.Type type, JObject obj)
	{
		var test = Prop(obj, "test", Expression);
		var ifTrue = Prop(obj, "ifTrue", Expression);
		var ifFalse = Prop(obj, "ifFalse", Expression);

		switch (nodeType)
		{
			case ExpressionType.Conditional:
				return Expr.Condition(test, ifTrue, ifFalse, type);
			default:
				throw new NotSupportedException();
		}
	}
}
