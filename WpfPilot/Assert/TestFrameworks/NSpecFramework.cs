namespace WpfPilot.Assert.TestFrameworks;

using System;
using System.Reflection;

internal class NSpecFramework : ITestFramework
{
	public bool IsAvailable
	{
		get
		{
			Assembly = Array.Find(AppDomain.CurrentDomain.GetAssemblies(), a => a.FullName!.StartsWith("nspec,", StringComparison.OrdinalIgnoreCase));
			if (Assembly is null)
				return false;

			int majorVersion = Assembly.GetName().Version!.Major;
			return majorVersion >= 2;
		}
	}

	public void Throw(string message)
	{
		Type exceptionType = Assembly?.GetType("NSpec.Domain.AssertionException")
			?? throw new NotSupportedException("Failed to create the NSpec assertion type");

		throw (Exception) Activator.CreateInstance(exceptionType, message)!;
	}

	private Assembly? Assembly;
}
