namespace Aq.ExpressionJsonSerializer;

using System.Linq.Expressions;

internal partial class Serializer
{
	private bool UnaryExpression(Expression expr)
	{
		var expression = expr as UnaryExpression;
		if (expression == null)
			return false;

		Prop("typeName", "unary");
		Prop("operand", Expression(expression.Operand));
		Prop("method", Method(expression.Method));

		return true;
	}
}
