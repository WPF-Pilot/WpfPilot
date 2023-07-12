namespace WpfPilot.Interop;

using System;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json.Serialization;

// Newtonsoft does not support using private/internal constructors for deserialization, so we need to use a custom contract resolver.
// https://stackoverflow.com/a/35865022/16617265
internal sealed class RobustContractResolver : DefaultContractResolver
{
	protected override JsonObjectContract CreateObjectContract(Type objectType)
	{
		var c = base.CreateObjectContract(objectType);

		if (c.DefaultCreator != null || c.OverrideCreator != null)
			return c;

		// Prefer longer constructors.
		var constructors = objectType.GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
			.Where(x => x.GetParameters().Length <= c.Properties.Count)
			.OrderBy(x => x.GetParameters().Length)
			.ToList();

		// Prefer public constructors, then internal, then private. `IsAssembly` == internal.
		constructors.Sort((x, y) =>
		{
			var xAccess = x.IsPublic ? 0 : x.IsAssembly ? 1 : 2;
			var yAccess = y.IsPublic ? 0 : y.IsAssembly ? 1 : 2;
			return xAccess - yAccess;
		});

		var constructor = constructors.FirstOrDefault(constructorInfo =>
		{
			var parameters = CreateConstructorParameters(constructorInfo, c.Properties);
			return parameters.Count == constructorInfo.GetParameters().Length;
		});

		if (constructor != null)
		{
			c.OverrideCreator = args => constructor.Invoke(args);
			c.CreatorParameters.Clear();
			foreach (var parameter in CreateConstructorParameters(constructor, c.Properties))
				c.CreatorParameters.Add(parameter);
		}

		return c;
	}
}
