namespace Aq.ExpressionJsonSerializer;

using System;
using System.Linq.Expressions;

internal partial class Serializer
{
	private bool ListInitExpression(Expression expr)
	{
		var expression = expr as DefaultExpression;
		if (expression == null)
			return false;

		throw new NotImplementedException();
	}
}
