namespace WpfPilot.Utility;

using System;
using System.Collections.Generic;
using System.Linq.Expressions;

internal static class ExpressionExtensions
{
	public static IEnumerable<Expression> Flatten(this Expression expression)
	{
		return Visitor.Flatten(expression);
	}

	private sealed class Visitor : ExpressionVisitor
	{
		private Visitor(Action<Expression> nodeAction)
		{
			NodeAction = nodeAction;
		}

		public override Expression Visit(Expression? node)
		{
			NodeAction(node);
			return base.Visit(node);
		}

		public static IEnumerable<Expression> Flatten(Expression expression)
		{
			var result = new List<Expression>();
			var visitor = new Visitor(t => result.Add(t));
			visitor.Visit(expression);
			return result;
		}

		private readonly Action<Expression> NodeAction;
	}
}
