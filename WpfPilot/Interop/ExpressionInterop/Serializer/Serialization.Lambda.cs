namespace Aq.ExpressionJsonSerializer;

using System.Linq.Expressions;

internal partial class Serializer
{
	private bool LambdaExpression(Expression expr)
	{
		var expression = expr as LambdaExpression;
		if (expression == null)
			return false;

		Prop("typeName", "lambda");
		Prop("name", expression.Name);
		Prop("parameters", Enumerable(expression.Parameters, Expression));
		Prop("body", Expression(expression.Body));
		Prop("tailCall", expression.TailCall);

		return true;
	}
}
