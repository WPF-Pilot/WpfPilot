namespace Aq.ExpressionJsonSerializer;

using System.Linq.Expressions;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

internal class ExpressionJsonConverter : JsonConverter
{
	private static readonly System.Type TypeOfExpression = typeof(Expression);

	public ExpressionJsonConverter(Assembly resolvingAssembly)
	{
		Assembly = resolvingAssembly;
	}

	public override bool CanConvert(System.Type objectType)
	{
		return objectType == TypeOfExpression || objectType.IsSubclassOf(TypeOfExpression);
	}

	public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
	{
		Serializer.Serialize(writer, serializer, (Expression?) value);
	}

	public override object ReadJson(JsonReader reader, System.Type objectType, object? existingValue, JsonSerializer serializer)
	{
		return Deserializer.Deserialize(Assembly, JToken.ReadFrom(reader));
	}

	private readonly Assembly Assembly;
}
