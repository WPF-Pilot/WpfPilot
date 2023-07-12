namespace WpfPilot.Assert.TestFrameworks;

using System;
using System.Reflection;

internal class XUnit2TestFramework : ITestFramework
{
	public bool IsAvailable
	{
		get
		{
			try
			{
				// For netfx the assembly is not in AppDomain by default, so we can't just scan AppDomain.CurrentDomain
				Assembly = Assembly.Load(new AssemblyName("xunit.assert"));
				return Assembly is not null;
			}
			catch
			{
				return false;
			}
		}
	}

	public void Throw(string message)
	{
		Type exceptionType = Assembly?.GetType("Xunit.Sdk.XunitException")
			?? throw new NotSupportedException("Failed to create the XUnit assertion type");

		throw (Exception) Activator.CreateInstance(exceptionType, message)!;
	}

	private Assembly? Assembly;
}
