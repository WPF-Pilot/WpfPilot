namespace WpfPilot.Utility;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

internal static class ReflectionUtility
{
	public static object? FindAndInvokeBestMatch(IReadOnlyList<MethodInfo> methods, object? target, object?[]? args)
	{
		Exception finalException = new("Failed to match any methods.");

		foreach (var method in methods)
		{
			object? result;
			try
			{
				result = method.Invoke(target, args);
				return result;
			}
			catch (ArgumentException e)
			{
				finalException = e;

				// Ignore System.ArgumentException: 'Object of type 'X' cannot be converted to type 'Y'.' exceptions
				// and try the next method.
				if (e.Message?.Contains("cannot be converted to type") == true)
					continue;
			}
			catch (TargetParameterCountException e)
			{
				finalException = e;
				continue;
			}
		}

		throw finalException;
	}

	public static IReadOnlyList<MethodInfo> GetCandidateMethods(Type type, string methodName, BindingFlags bindingFlags, object?[]? args)
	{
		args ??= new object[0];

		var methods = type.GetMethods(bindingFlags)
			.Where(x => x.Name == methodName)
			.Where(x => x.GetParameters().Length == args.Length)
			.Where(x => ParametersMatch(x.GetParameters(), args!))
			.ToList();

		methods.Sort((x, y) =>
		{
			// Prefer public methods, then internal, then private. `IsAssembly` == internal.
			var xAccess = x.IsPublic ? 0 : x.IsAssembly ? 1 : 2;
			var yAccess = y.IsPublic ? 0 : y.IsAssembly ? 1 : 2;
			return xAccess - yAccess;
		});

		return methods.AsReadOnly();
	}

	private static bool ParametersMatch(ParameterInfo[] parameterInfos, object[] args)
	{
		for (var i = 0; i < parameterInfos.Length; i++)
		{
			// This technically does not make sense for value types, but we'll allow it to simplify the code.
			// We may improve it in the future.
			if (args[i] == null)
				continue;

			if (!parameterInfos[i].ParameterType.IsAssignableFrom(args[i]?.GetType()))
				return false;
		}

		return true;
	}
}
