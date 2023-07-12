namespace Aq.ExpressionJsonSerializer;

using System.Linq.Expressions;

internal partial class Serializer
{
	private bool LoopExpression(Expression expr)
	{
		var expression = expr as LoopExpression;
		if (expression == null)
			return false;

		Prop("typeName", "loop");
		Prop("body", Expression(expression.Body));

		if (expression.BreakLabel != null)
		{
			Prop("breakLabelName", expression.BreakLabel.Name ?? "#" + expression.BreakLabel.GetHashCode());
			Prop("breakLabeType", Type(expression.BreakLabel.Type));
		}

		if (expression.ContinueLabel != null)
		{
			Prop("continueLabelName", expression.ContinueLabel.Name ?? "#" + expression.ContinueLabel.GetHashCode());
			Prop("continueLabelType", Type(expression.ContinueLabel.Type));
		}

		return true;
	}
}
