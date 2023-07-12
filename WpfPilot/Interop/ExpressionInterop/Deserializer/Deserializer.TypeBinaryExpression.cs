namespace Aq.ExpressionJsonSerializer;

using System;
using System.Linq.Expressions;
using Newtonsoft.Json.Linq;
using Expr = System.Linq.Expressions.Expression;

internal partial class Deserializer
{
	private TypeBinaryExpression TypeBinaryExpression(ExpressionType nodeType, System.Type type, JObject obj)
	{
		var expression = Prop(obj, "expression", Expression);
		var typeOperand = Prop(obj, "typeOperand", Type);
		
		switch (nodeType)
		{
			case ExpressionType.TypeIs:
				return Expr.TypeIs(expression, typeOperand);
			case ExpressionType.TypeEqual:
				return Expr.TypeEqual(expression, typeOperand);
			default:
				throw new NotSupportedException();
		}
	}
}
