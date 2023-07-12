namespace WpfPilot;

using System;
using System.Linq;
using System.Reflection;
using WpfPilot.Utility;

public static class ReflectionExtensions
{
	public static T Invoke<T>(this Type type, string methodName, params object[]? args)
	{
		var methods = ReflectionUtility.GetCandidateMethods(type, methodName, InvokeAllBindings, args);
		if (methods.Count == 0)
			throw new ArgumentException($"No method with name `{methodName}` on type `{type.Name}` has a signature matching the given args.");

		var result = ReflectionUtility.FindAndInvokeBestMatch(methods, null, args);
		return (T) result!;
	}

	public static void Invoke(this Type type, string methodName, params object[]? args)
	{
		var methods = ReflectionUtility.GetCandidateMethods(type, methodName, InvokeAllBindings, args);
		if (methods.Count == 0)
			throw new ArgumentException($"No method with name `{methodName}` on type `{type.Name}` has a signature matching the given args.");

		ReflectionUtility.FindAndInvokeBestMatch(methods, null, args);
	}

	public static T InvokeOn<T>(this Type type, object? target, string methodName, params object[]? args)
	{
		var methods = ReflectionUtility.GetCandidateMethods(type, methodName, InvokeAllBindings, args);
		if (methods.Count == 0)
			throw new ArgumentException($"No method with name `{methodName}` on type `{type.Name}` has a signature matching the given args.");

		var result = ReflectionUtility.FindAndInvokeBestMatch(methods, target, args);
		return (T) result!;
	}

	public static void InvokeOn(this Type type, object? target, string methodName, params object[]? args)
	{
		var methods = ReflectionUtility.GetCandidateMethods(type, methodName, InvokeAllBindings, args);
		if (methods.Count == 0)
			throw new ArgumentException($"No method with name `{methodName}` on type `{type.Name}` has a signature matching the given args.");

		ReflectionUtility.FindAndInvokeBestMatch(methods, target, args);
	}

	public static T Invoke<T>(this object obj, string methodName, params object[]? args)
	{
		var methods = ReflectionUtility.GetCandidateMethods(obj.GetType(), methodName, InvokeAllBindings, args);
		if (methods.Count == 0)
			throw new ArgumentException($"No method with name `{methodName}` on type `{obj.GetType().Name}` has a signature matching the given args.");

		var result = ReflectionUtility.FindAndInvokeBestMatch(methods, obj, args);
		return (T) result!;
	}

	public static void Invoke(this object obj, string methodName, params object[]? args)
	{
		var methods = ReflectionUtility.GetCandidateMethods(obj.GetType(), methodName, InvokeAllBindings, args);
		if (methods.Count == 0)
			throw new ArgumentException($"No method with name `{methodName}` on type `{obj.GetType().Name}` has a signature matching the given args.");

		ReflectionUtility.FindAndInvokeBestMatch(methods, obj, args);
	}

	public static T Field<T>(this object obj, string fieldName)
	{
		var fieldInfos = obj.GetType().GetFields(InvokeInstanceBindings)
			.Where(x => x.Name == fieldName && x.FieldType == typeof(T))
			.ToList();

		fieldInfos.Sort((x, y) =>
		{
			// Prefer public fields, then internal, then private. `IsAssembly` == internal.
			var xAccess = x.IsPublic ? 0 : x.IsAssembly ? 1 : 2;
			var yAccess = y.IsPublic ? 0 : y.IsAssembly ? 1 : 2;
			return xAccess - yAccess;
		});

		var fieldInfo = fieldInfos.FirstOrDefault() ?? throw new ArgumentException($"No field found with name `{fieldName}` on type `{obj.GetType().Name}`.");

		return (T) fieldInfo.GetValue(obj)!;
	}

	public static T Property<T>(this object obj, string propertyName)
	{
		var propertyInfos = obj.GetType().GetProperties(InvokeInstanceBindings)
			.Where(x => x.Name == propertyName && x.PropertyType == typeof(T))
			.ToList();

		var propertyInfo = propertyInfos.FirstOrDefault() ?? throw new ArgumentException($"No property found with name `{propertyName}` on type `{obj.GetType().Name}`.");

		return (T) propertyInfo.GetValue(obj)!;
	}

	private const BindingFlags InvokeAllBindings = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy;
	private const BindingFlags InvokeInstanceBindings = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy;
}
