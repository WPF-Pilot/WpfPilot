namespace Aq.ExpressionJsonSerializer;

using System.Linq.Expressions;

internal partial class Serializer
{
	private bool NewExpression(Expression expr)
	{
		var expression = expr as NewExpression;
		if (expression == null)
			return false;

		Prop("typeName", "new");
		Prop("constructor", Constructor(expression.Constructor));
		Prop("arguments", Enumerable(expression.Arguments, Expression));
		Prop("members", Enumerable(expression.Members, Member));

		return true;
	}
}
