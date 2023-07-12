namespace Aq.ExpressionJsonSerializer;

using System.Linq.Expressions;

internal partial class Serializer
{
	private bool BinaryExpression(Expression expr)
	{
		var expression = expr as BinaryExpression;
		if (expression == null)
			return false;

		Prop("typeName", "binary");
		Prop("left", Expression(expression.Left));
		Prop("right", Expression(expression.Right));
		Prop("method", Method(expression.Method));
		Prop("conversion", Expression(expression.Conversion));
		Prop("liftToNull", expression.IsLiftedToNull);

		return true;
	}
}
