namespace Aq.ExpressionJsonSerializer;

using System;
using System.Linq.Expressions;
using Newtonsoft.Json.Linq;
using Expr = System.Linq.Expressions.Expression;

internal partial class Deserializer
{
	private RuntimeVariablesExpression RuntimeVariablesExpression(ExpressionType nodeType, System.Type type, JObject obj)
	{
		var variables = Prop(obj, "variables", Enumerable(ParameterExpression));

		switch (nodeType)
		{
			case ExpressionType.RuntimeVariables:
				return Expr.RuntimeVariables(variables!);
			default:
				throw new NotSupportedException();
		}
	}
}
