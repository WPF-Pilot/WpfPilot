namespace Aq.ExpressionJsonSerializer;

using System.Linq;
using System.Linq.Expressions;

internal partial class Serializer
{
	private bool BlockExpression(Expression expr)
	{
		var expression = expr as BlockExpression;
		if (expression == null)
			return false;

		Prop("typeName", "block");
		Prop("expressions", Enumerable(expression.Expressions, Expression));

		var variables = expression.Variables.ToList();

		// HACK: This seems to be necessary on NET Framework, but not NET for unknown reasons.
		if (expression.Result is ParameterExpression result && !variables.Contains(result))
			variables.Add(result);

		Prop("variables", Enumerable(variables, Expression));
		return true;
	}
}
