namespace Aq.ExpressionJsonSerializer;

using System;
using System.Linq.Expressions;

internal partial class Serializer
{
	private bool TryExpression(Expression expr)
	{
		var expression = expr as TryExpression;
		if (expression == null)
			return false;

		throw new NotImplementedException();
	}
}
