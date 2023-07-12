namespace Aq.ExpressionJsonSerializer;

using System;
using System.Linq.Expressions;
using Newtonsoft.Json.Linq;
using Expr = System.Linq.Expressions.Expression;

internal partial class Deserializer
{
	private MemberExpression MemberExpression(ExpressionType nodeType, System.Type type, JObject obj)
	{
		var expression = Prop(obj, "expression", Expression);
		var member = Prop(obj, "member", Member);

		switch (nodeType)
		{
			case ExpressionType.MemberAccess:
				return Expr.MakeMemberAccess(expression, member);
			default:
				throw new NotSupportedException();
		}
	}
}
