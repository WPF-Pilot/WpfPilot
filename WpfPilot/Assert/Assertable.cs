namespace WpfPilot.Assert;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using WpfPilot;
using WpfPilot.Assert.TestFrameworks;

public sealed class Assertable
{
	private Assertable(Element value, Expression valueExpression)
	{
		Value = value;
		ValueExpression = valueExpression;
	}

	public Assertable IsTrue(Expression<Func<Element, bool?>> predicateExpression)
	{
		if (predicateExpression == null)
			throw new ArgumentNullException(nameof(predicateExpression));

		if (!IsNoOp)
		{
			var getValueExpression = CoalesceValueWith(predicateExpression);
			var message = GetMessageIfFalse(getValueExpression);

			if (message != null)
				TestFrameworkProvider.Throw(message);
		}

		return this;
	}

	public Element Value { get; }

	public static implicit operator Element(Assertable source) => source?.Value ?? throw new ArgumentNullException(nameof(source));

	internal static Assertable FromValueExpression(Element value, Expression valueExpression)
		=> new(value, valueExpression);

	internal static string? GetMessageIfFalse(Expression<Func<bool?>> predicateExpression)
	{
		var predicateFunc = predicateExpression.Compile();
		Exception? e = null;

		try
		{
			if (predicateFunc.Invoke().GetValueOrDefault())
				return null;
		}
		catch (Exception exception)
		{
			e = exception;
		}

		return ElementAssertExtensions.GetDiagnosticMessage(predicateExpression.Body, e);
	}

	private bool IsNoOp => Value == null;

	private Expression<Func<TResult>> CoalesceValueWith<TResult>(Expression<Func<Element, TResult>> mapExpression)
	{
		var resultExpression = new ReplaceParameterWithExpressionVisitor(mapExpression.Parameters, ValueExpression).Visit(mapExpression.Body);
		return Expression.Lambda<Func<TResult>>(resultExpression);
	}

	private sealed class ReplaceParameterWithExpressionVisitor : ExpressionVisitor
	{
		public ReplaceParameterWithExpressionVisitor(IEnumerable<ParameterExpression> oldParameters, Expression newExpression)
		{
			ParameterMap = oldParameters.ToDictionary(p => p, _ => newExpression);
		}

		protected override Expression VisitParameter(ParameterExpression parameter) =>
			ParameterMap.TryGetValue(parameter, out var replacement)
				? replacement
				: base.VisitParameter(parameter);

		private readonly IDictionary<ParameterExpression, Expression> ParameterMap;
	}

	private readonly Expression ValueExpression;
}
