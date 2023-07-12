namespace Aq.ExpressionJsonSerializer;

using System;
using System.Linq.Expressions;
using Newtonsoft.Json.Linq;
using Expr = System.Linq.Expressions.Expression;

internal partial class Deserializer
{
	private NewExpression NewExpression(ExpressionType nodeType, System.Type type, JObject obj)
	{
		var constructor = Prop(obj, "constructor", Constructor);
		var arguments = Prop(obj, "arguments", Enumerable(Expression));
		var members = Prop(obj, "members", Enumerable(Member));

		switch (nodeType)
		{
			case ExpressionType.New:
				if (arguments == null)
				{
					if (members == null)
						return Expr.New(constructor);

					return Expr.New(constructor, new Expression[0], members!);
				}

				if (members == null)
					return Expr.New(constructor, arguments!);

				return Expr.New(constructor, arguments!, members!);
			default:
				throw new NotSupportedException();
		}
	}
}
