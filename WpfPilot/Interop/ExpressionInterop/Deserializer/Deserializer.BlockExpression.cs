namespace Aq.ExpressionJsonSerializer;

using System;
using System.Linq.Expressions;
using Newtonsoft.Json.Linq;
using Expr = System.Linq.Expressions.Expression;

internal partial class Deserializer
{
	private BlockExpression BlockExpression(ExpressionType nodeType, System.Type type, JObject obj)
	{
		var expressions = Prop(obj, "expressions", Enumerable(Expression));
		var variables = Prop(obj, "variables", Enumerable(ParameterExpression));

		switch (nodeType)
		{
			case ExpressionType.Block:
				return Expr.Block(type!, variables!, expressions!);
			default:
				throw new NotSupportedException();
		}
	}
}
