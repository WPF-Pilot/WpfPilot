namespace WpfPilot.Tests;

using NUnit.Framework;
using WpfPilot.AppDriverPayload.Commands;
using WpfPilot.Utility;

[TestFixture]
public sealed class ReflectionUtilityTests
{
	[Test]
	public void TestGetAndInvokeMethod()
	{
		var layEggMethod = ReflectionUtility.GetCandidateMethods(typeof(Mocks.Chicken), "LayEgg", InvokeCommand.InvokeCommandBindings, null);
		var flyMethod = ReflectionUtility.GetCandidateMethods(typeof(Mocks.Chicken), "Fly", InvokeCommand.InvokeCommandBindings, new object[0]);
		var nameMethod = ReflectionUtility.GetCandidateMethods(typeof(Mocks.Chicken), "Name", InvokeCommand.InvokeCommandBindings, new object[0]);

		Assert.IsNotEmpty(layEggMethod);
		Assert.IsNotEmpty(flyMethod);
		Assert.IsNotEmpty(nameMethod);

		var chicken = new Mocks.Chicken();
		Assert.DoesNotThrow(() => ReflectionUtility.FindAndInvokeBestMatch(layEggMethod, chicken, null));
	}

	[Test]
	public void TestMethodWithNullArgs()
	{
		string? nullString = null;

		var layEggMethod = ReflectionUtility.GetCandidateMethods(typeof(Mocks.Chicken), "NullParamsCheck", InvokeCommand.InvokeCommandBindings, new object[] { nullString!, "Hello world" });

		Assert.IsNotEmpty(layEggMethod);
	}
}
