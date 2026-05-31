namespace WpfPilot.Mac.Driver;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

/// <summary>
/// Parses the constrained lambdas accepted by the macOS <c>Element.Invoke</c>/<c>InvokeAsync</c> API
/// into a method name plus evaluated scalar arguments.
/// </summary>
/// <remarks>
/// This deliberately avoids serializing the expression tree itself (the Windows approach), which is
/// fragile across process/assembly boundaries because it must reconstruct CLR types by assembly identity
/// on the far side. The supported shapes are a single instance method call (<c>x =&gt; x.Foo(a, b)</c>) or
/// a property read (<c>x =&gt; x.Bar</c>); arguments are evaluated locally to constants before transport.
/// </remarks>
internal static class MethodCallParser
{
	public static (string Method, IReadOnlyList<object?> Args) Parse(LambdaExpression lambda)
	{
		var body = Unwrap(lambda.Body);

		if (body is MethodCallExpression call)
		{
			if (call.Object is null)
				throw new NotSupportedException("Only instance method calls on the element are supported.");
			var args = call.Arguments.Select(EvaluateArgument).ToList();
			return (call.Method.Name, args);
		}

		if (body is MemberExpression member && member.Member is System.Reflection.PropertyInfo property)
			return ("get_" + property.Name, Array.Empty<object?>());

		throw new NotSupportedException(
			$"Unsupported invoke expression '{lambda.Body}'. Use a single method call (x => x.Method(args)) or property read (x => x.Property).");
	}

	private static Expression Unwrap(Expression expression)
	{
		// Strip the implicit conversions the compiler inserts (e.g. Task<string> -> Task, value -> object).
		while (expression is UnaryExpression unary && (unary.NodeType == ExpressionType.Convert || unary.NodeType == ExpressionType.ConvertChecked))
			expression = unary.Operand;
		return expression;
	}

	private static object? EvaluateArgument(Expression argument)
	{
		if (argument is ConstantExpression constant)
			return constant.Value;

		// Evaluate captured locals / member accesses by compiling the sub-expression.
		var converted = Expression.Convert(argument, typeof(object));
		var lambda = Expression.Lambda<Func<object?>>(converted);
		return lambda.Compile().Invoke();
	}
}
