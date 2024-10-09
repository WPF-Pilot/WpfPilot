namespace WpfPilot.Assert;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using WpfPilot;
using WpfPilot.Assert.TestFrameworks;

public sealed class Assertable
{
	private Assertable(Element value, Expression valueExpression, Action onCheck)
	{
		Value = value;
		ValueExpression = valueExpression;
		OnCheck = onCheck;
	}

	public Assertable IsTrue(Expression<Func<Element, bool?>> predicateExpression, int timeoutMs = 5_000)
	{
		if (predicateExpression == null)
			throw new ArgumentNullException(nameof(predicateExpression));

		if (IsNoOp)
			return this;

		var startTime = DateTime.UtcNow;
		string? message = null;
		do
		{
			var getValueExpression = CoalesceValueWith(predicateExpression);
			message = GetMessageIfFalse(getValueExpression);

			if (message == null)
			{
				// Assertion passed.
				break;
			}
			else
			{
				// Wait for 1 second before retrying.
				OnCheck();
				Task.Delay(1000).GetAwaiter().GetResult();
			}
		}
		while ((DateTime.UtcNow - startTime).TotalMilliseconds < timeoutMs);

		if (message != null)
			TestFrameworkProvider.Throw(message);

		return this;
	}

	public Element Value { get; }

	public static implicit operator Element(Assertable source) => source?.Value ?? throw new ArgumentNullException(nameof(source));

	internal static Assertable FromValueExpression(Element value, Expression valueExpression, Action onCheck)
		=> new(value, valueExpression, onCheck);

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
	private readonly Action OnCheck;
}
