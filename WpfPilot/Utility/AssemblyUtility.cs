#pragma warning disable

namespace WpfPilot.Utility;

using System;
using System.Reflection;

internal static class AssemblyUtility
{
	// Load an assembly by name, trying the full name first, then the partial name.
	// This is useful for scenarios where the Test Suite is running on a slightly different version than the target app,
	// since the assemblies may be near identical, but have different versions.
	public static Assembly LoadAssembly(string fullAssemblyName)
	{
		// A full assembly name looks like: `UtilityLibrary, Version=1.1.0.0, Culture=neutral, PublicKeyToken=null`
		try
		{
			return Assembly.Load(fullAssemblyName);
		}
		catch
		{
			// Get the partial name from the full name.
			var partialName = fullAssemblyName.Split(',')[0];

			// LoadWithPartialName is deprecated, but is far simpler than the alternative.
			// When LoadWithPartialName is removed, we can use a strategy such as:
			// https://web.archive.org/web/20230527221640/https://dotnetcoretutorials.com/getting-assemblies-is-harder-than-you-think-in-c/
			return Assembly.LoadWithPartialName(partialName);
		}
	}

	public static Type? LoadType(
		string assemblyQualifiedTypeName, // Eg `System.Int32, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089`.
		string typeName, // Eg `MyApp.CoolClass`
		string fullAssemblyName) // Eg `UtilityLibrary, Version=1.1.0.0, Culture=neutral, PublicKeyToken=null`
	{
		var type = Type.GetType(assemblyQualifiedTypeName);
		if (type != null)
			return type;

		var assembly = LoadAssembly(fullAssemblyName);
		return assembly.GetType(typeName);
	}
}
