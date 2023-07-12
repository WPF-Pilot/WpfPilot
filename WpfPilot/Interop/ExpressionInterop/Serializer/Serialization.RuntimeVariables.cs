namespace Aq.ExpressionJsonSerializer;

using System.Linq.Expressions;

internal partial class Serializer
{
	private bool RuntimeVariablesExpression(Expression expr)
	{
		var expression = expr as RuntimeVariablesExpression;
		if (expression == null)
			return false;

		Prop("typeName", "runtimeVariables");
		Prop("variables", Enumerable(expression.Variables, Expression));

		return true;
	}
}
