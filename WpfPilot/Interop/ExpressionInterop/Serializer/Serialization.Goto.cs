namespace Aq.ExpressionJsonSerializer;

using System.Linq.Expressions;

internal partial class Serializer
{
	private bool GotoExpression(Expression expr)
	{
		var expression = expr as GotoExpression;
		if (expression == null)
			return false;

		Prop("typeName", "goto");
		Prop("value", Expression(expression.Value));
		Prop("kind", Enum(expression.Kind));
		Prop("targetName", expression.Target.Name ?? "#" + expression.Target.GetHashCode());
		Prop("targetType", Type(expression.Target.Type));

		return true;
	}
}
