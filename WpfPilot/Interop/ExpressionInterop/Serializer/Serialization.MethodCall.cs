namespace Aq.ExpressionJsonSerializer;

using System;
using System.Linq.Expressions;

internal partial class Serializer
{
	private bool MethodCallExpression(Expression expr)
	{
		var expression = expr as MethodCallExpression;
		if (expression == null)
			return false;

		var isTaskDeclaringType = expression.Method.DeclaringType?.Name.StartsWith("Task") ?? false;
		if (expression.Method.Name == "GetAwaiter" && isTaskDeclaringType)
		{
			throw new InvalidOperationException(@"GetAwaiter() will cause a deadlock.
Use the async version of the given method.
EG Invoke(() => FooAsync().GetAwaiter().GetResult()) -> InvokeAsync(() => FooAsync())");
		}

		if (expression.Method.Name == "Wait" && isTaskDeclaringType)
		{
			throw new InvalidOperationException(@"Wait() will cause a deadlock.
Use the async version of the given method.
EG Invoke(() => FooAsync().Wait()) -> InvokeAsync(() => FooAsync())");
		}

		Prop("typeName", "methodCall");
		Prop("object", Expression(expression.Object));
		Prop("method", Method(expression.Method));
		Prop("arguments", Enumerable(expression.Arguments, Expression));

		return true;
	}
}
