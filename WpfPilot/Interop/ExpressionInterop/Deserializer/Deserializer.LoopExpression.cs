namespace Aq.ExpressionJsonSerializer;

using System.Linq.Expressions;
using Newtonsoft.Json.Linq;
using Expr = System.Linq.Expressions.Expression;

internal partial class Deserializer
{
	private LoopExpression LoopExpression(ExpressionType nodeType, System.Type type, JObject obj)
	{
		var body = Prop(obj, "body", Expression);

		var breakName = Prop<string>(obj, "breakLabelName");
		var breakType = Prop(obj, "breakLabeType", Type);

		var continueName = Prop<string>(obj, "continueLabelName");
		var continueType = Prop(obj, "continueLabelType", Type);

		if (continueType != null)
			return Expr.Loop(body, CreateLabelTarget(breakName, breakType), CreateLabelTarget(continueName, continueType));    

		return Expr.Loop(body, CreateLabelTarget(breakName, breakType));
	}
}
