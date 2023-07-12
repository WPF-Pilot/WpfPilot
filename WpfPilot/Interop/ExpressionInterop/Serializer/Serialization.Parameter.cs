namespace Aq.ExpressionJsonSerializer;

using System;
using System.Collections.Generic;
using System.Linq.Expressions;

internal partial class Serializer
{
	private readonly Dictionary<ParameterExpression, string>
		ParameterExpressions = new Dictionary<ParameterExpression, string>();

	private bool ParameterExpression(Expression expr)
	{
		var expression = expr as ParameterExpression;
		if (expression == null)
			return false;

		string? name;
		if (!ParameterExpressions.TryGetValue(expression, out name))
		{
			name = expression.Name ?? "p_" + Guid.NewGuid().ToString("N");
			ParameterExpressions[expression] = name;
		}

		Prop("typeName", "parameter");
		Prop("name", name);

		return true;
	}
}
