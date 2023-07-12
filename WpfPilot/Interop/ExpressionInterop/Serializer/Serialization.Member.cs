namespace Aq.ExpressionJsonSerializer;

using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

internal partial class Serializer
{
	private bool MemberExpression(Expression expr)
	{
		var expression = expr as MemberExpression;
		if (expression == null)
			return false;

		var isTaskResult = expression.Expression?.Type.Name.StartsWith("Task") ?? false;
		if (isTaskResult && expression.Member.Name == "Result")
		{
			throw new InvalidOperationException(@"Task.Result will cause a deadlock.
Use the async version of the given method.
EG Invoke(() => FooAsync().Result) -> InvokeAsync(() => FooAsync())");
		}

		Prop("typeName", "member");
		Prop("expression", Expression(expression.Expression));
		Prop("member", Member(expression.Member));

		return true;
	}

	// Copied from `Serialize.Linq`.
	private bool TryGetConstantValueFromMemberExpression(MemberExpression memberExpression, out object? constantValue, out Type? constantValueType)
	{
		constantValue = null;
		constantValueType = null;
		FieldInfo? parentField = null;
		while (true)
		{
			var run = memberExpression;
			while (true)
			{
				if (!(run.Expression is MemberExpression next))
					break;
				run = next;
			}

			switch (memberExpression.Member)
			{
				case FieldInfo field:
					{
						if (memberExpression.Expression != null)
						{
							if (memberExpression.Expression.NodeType == ExpressionType.Constant)
							{
								var constantExpression = (ConstantExpression) memberExpression.Expression;
								constantValue = constantExpression.Value;
								constantValueType = constantExpression.Type;
								var match = false;
								do
								{
									var fields = constantValueType.GetFields();
									var memberField = fields.Length > 0
										? fields.SingleOrDefault(n => field.Name.Equals(n.Name, StringComparison.Ordinal))
										: null;
									if (memberField == null && parentField != null)
									{
										memberField = fields.Length > 1
											? fields.SingleOrDefault(n => parentField?.Name.Equals(n.Name, StringComparison.Ordinal) ?? false)
											: fields.FirstOrDefault();
									}

									if (memberField == null)
										break;

									constantValueType = memberField.FieldType;
									constantValue = memberField.GetValue(constantValue);
									match = true;
								}
								while (constantValue != null);

								return match;
							}

							if (memberExpression.Expression is MemberExpression subExpression)
							{
								memberExpression = subExpression;
								parentField = parentField ?? field;
								continue;
							}
						}

						if (field.IsPrivate || field.IsFamilyAndAssembly)
						{
							constantValue = field.GetValue(null);
							return true;
						}

						break;
					}

				case PropertyInfo propertyInfo:
					try
					{
						constantValue = System.Linq.Expressions.Expression.Lambda(memberExpression).Compile().DynamicInvoke();
						constantValueType = propertyInfo.PropertyType;
						return true;
					}
					catch (InvalidOperationException)
					{
						constantValue = null;
						return false;
					}

				default:
					throw new NotSupportedException("MemberType '" + memberExpression.Member.GetType().Name + "' not yet supported.");
			}

			return false;
		}
	}
}
