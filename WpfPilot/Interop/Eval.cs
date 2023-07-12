namespace WpfPilot.Interop;

using System.Linq.Expressions;
using System.Reflection;
using Aq.ExpressionJsonSerializer;
using Newtonsoft.Json;

internal sealed class Eval
{
	public Eval(string expressionJson)
	{
		ExpressionJson = expressionJson;
	}

	private Eval(LambdaExpression expression)
	{
		ExpressionJson = JsonConvert.SerializeObject(expression, Settings);
	}

	public static Eval SerializeCode(LambdaExpression expression) =>
		new(expression);

	public string Type { get; } = EvalType;
	public string ExpressionJson { get; }
	public const string EvalType = "p:Eval";

	private static readonly JsonSerializerSettings Settings = new()
	{
		ContractResolver = new RobustContractResolver(),
		ConstructorHandling = ConstructorHandling.AllowNonPublicDefaultConstructor,
		Converters = { new ExpressionJsonConverter(Assembly.GetExecutingAssembly()) }
	};
}
