namespace Aq.ExpressionJsonSerializer;

using System.Linq.Expressions;

internal partial class Serializer
{
	private bool ConstantExpression(Expression expr)
	{
		var expression = expr as ConstantExpression;
		if (expression == null)
			return false;

		Prop("typeName", "constant");
		if (expression.Value == null)
		{
			Prop("value", () => Writer.WriteNull());
		}
		else
		{
			var value = expression.Value;
			var type = value.GetType();
			Prop("value", () =>
			{
				Writer.WriteStartObject();
				Prop("type", Type(type));
				Prop("value", Serialize(value, type));
				Writer.WriteEndObject();
			});
		}

		return true;
	}
}
