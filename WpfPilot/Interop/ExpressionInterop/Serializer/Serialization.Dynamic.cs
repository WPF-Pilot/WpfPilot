namespace Aq.ExpressionJsonSerializer;

using System;
using System.Linq.Expressions;

internal partial class Serializer
{
	private bool DynamicExpression(Expression expr)
	{
		var expression = expr as DynamicExpression;
		if (expression == null)
			return false;

		throw new NotImplementedException();
	}
}
