namespace Aq.ExpressionJsonSerializer;

using System;
using System.Linq.Expressions;
using Newtonsoft.Json.Linq;
using Expr = System.Linq.Expressions.Expression;

internal partial class Deserializer
{
	private GotoExpression GotoExpression(ExpressionType nodeType, System.Type type, JObject obj)
	{
		var value = Expression(Prop(obj, "value"));
		var kind = Enum<GotoExpressionKind>(Prop(obj, "kind"));
		var targetType = Type(Prop(obj, "targetType"));
		var targetName = Prop(obj, "targetName").Value<string>();

		switch (kind)
		{
			case GotoExpressionKind.Break:
				return Expr.Break(CreateLabelTarget(targetName, targetType), value);
			case GotoExpressionKind.Continue:
				return Expr.Continue(CreateLabelTarget(targetName, targetType));
			case GotoExpressionKind.Goto:
				return Expr.Goto(CreateLabelTarget(targetName, targetType), value);
			case GotoExpressionKind.Return:
				return Expr.Return(CreateLabelTarget(targetName, targetType), value);
			default:
				throw new NotImplementedException();
		}
	}
}
