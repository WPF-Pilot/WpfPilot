namespace Aq.ExpressionJsonSerializer;

using System;
using System.Linq.Expressions;
using Newtonsoft.Json.Linq;
using Expr = System.Linq.Expressions.Expression;

internal partial class Deserializer
{
	private InvocationExpression InvocationExpression(ExpressionType nodeType, System.Type type, JObject obj)
	{
		var expression = Prop(obj, "expression", Expression);
		var arguments = Prop(obj, "arguments", Enumerable(Expression));

		switch (nodeType)
		{
			case ExpressionType.Invoke:
				if (arguments == null)
					return Expr.Invoke(expression);

				return Expr.Invoke(expression!, arguments!);
			default:
				throw new NotSupportedException();
		}
	}
}
