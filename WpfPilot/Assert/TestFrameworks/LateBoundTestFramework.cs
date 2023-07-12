namespace WpfPilot.Assert.TestFrameworks;

using System;
using System.Reflection;

internal abstract class LateBoundTestFramework : ITestFramework
{
	public bool IsAvailable
	{
		get
		{
			string prefix = AssemblyName + ",";

			Assembly = Array.Find(AppDomain.CurrentDomain
				.GetAssemblies(), a => a.FullName!.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));

			return Assembly is not null;
		}
	}

	public void Throw(string message)
	{
		Type? exceptionType = Assembly?.GetType(ExceptionFullName);
		if (exceptionType is null)
			throw new NotSupportedException($"Failed to create the assertion exception for the current test framework: \"{ExceptionFullName}, {Assembly?.FullName}\"");

		throw (Exception) Activator.CreateInstance(exceptionType, message)!;
	}

	protected internal abstract string AssemblyName { get; }

	protected abstract string ExceptionFullName { get; }

	private Assembly? Assembly;
}
