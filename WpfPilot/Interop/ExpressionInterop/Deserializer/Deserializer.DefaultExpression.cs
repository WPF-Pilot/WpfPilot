﻿namespace Aq.ExpressionJsonSerializer;

using System;
using System.Linq.Expressions;
using Newtonsoft.Json.Linq;
using Expr = System.Linq.Expressions.Expression;

internal partial class Deserializer
{
	private DefaultExpression DefaultExpression(ExpressionType nodeType, System.Type type, JObject obj)
	{
		switch (nodeType)
		{
			case ExpressionType.Default:
				return Expr.Default(type);
			default:
				throw new NotSupportedException();
		}
	}
}
