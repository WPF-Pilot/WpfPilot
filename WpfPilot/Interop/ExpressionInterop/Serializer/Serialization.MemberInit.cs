namespace Aq.ExpressionJsonSerializer;

using System;
using System.Linq.Expressions;

internal partial class Serializer
{
	private bool MemberInitExpression(Expression expr)
	{
		var expression = expr as MemberInitExpression;
		if (expression == null)
			return false;

		throw new NotImplementedException();
	}
}
