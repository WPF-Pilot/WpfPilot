namespace WpfPilot.Tests;

using System;
using System.Linq.Expressions;
using WpfPilot.ExampleApp;

public static class AppDriverExtensions
{
	public static T RunCode<T>(this AppDriver appDriver, Expression<Func<App, T>> code)
	{
		return appDriver.GetElement(x => x.TypeName == nameof(App)).Invoke<App, T>(code);
	}
}
