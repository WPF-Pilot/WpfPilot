namespace WpfPilot.Tests;

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using WpfPilot;
using WpfPilot.ExampleApp;
using WpfPilot.Tests.TestUtility;
using WpfPilot.Utility.WpfUtility;

[TestFixture]
public sealed class AppDriverTests : AppTestBase
{
	[OneTimeSetUp]
	public override void Setup()
	{
		base.Setup();
		ExePath = $"../../../../../WpfPilot.ExampleApp/bin/x64/Debug/{CurrentFramework.GetVersion()}/WpfPilot.ExampleApp.exe";
		ExePath = Path.GetFullPath(ExePath);
		Assert.True(File.Exists(ExePath), $"Could not find ExampleApp at {ExePath}. Ensure the project has been compiled.");
	}

	[Test]
	public void TestLaunchOptions()
	{
		var appDriverLaunch = AppDriver.Launch(ExePath);
		Assert.IsNotNull(Process.GetProcessesByName("WpfPilot.ExampleApp").FirstOrDefault());
		appDriverLaunch.Dispose();
		WaitUntil(() => Process.GetProcessesByName("WpfPilot.ExampleApp").FirstOrDefault() == null);
		Assert.IsNull(Process.GetProcessesByName("WpfPilot.ExampleApp").FirstOrDefault());

		var appDriverLaunchWithArgs = AppDriver.Launch(ExePath, args: @"/debugmode /logpath c:\temp");
		Assert.IsNotNull(Process.GetProcessesByName("WpfPilot.ExampleApp").FirstOrDefault());
		appDriverLaunchWithArgs.Dispose();
		WaitUntil(() => Process.GetProcessesByName("WpfPilot.ExampleApp").FirstOrDefault() == null);
		Assert.IsNull(Process.GetProcessesByName("WpfPilot.ExampleApp").FirstOrDefault());

		var processStartInfo1 = new ProcessStartInfo(ExePath);
		var process1 = Process.Start(processStartInfo1);
		var appDriverAttachById = AppDriver.AttachTo(process1!.Id);
		Assert.IsNotNull(Process.GetProcessesByName("WpfPilot.ExampleApp").FirstOrDefault());
		appDriverAttachById.Dispose();
		WaitUntil(() => Process.GetProcessesByName("WpfPilot.ExampleApp").FirstOrDefault() == null);
		Assert.IsNull(Process.GetProcessesByName("WpfPilot.ExampleApp").FirstOrDefault());

		var processStartInfo2 = new ProcessStartInfo(ExePath);
		var process2 = Process.Start(processStartInfo2);
		var appDriverAttachByName = AppDriver.AttachTo(process2!.ProcessName);
		Assert.IsNotNull(Process.GetProcessesByName("WpfPilot.ExampleApp").FirstOrDefault());
		appDriverAttachByName.Dispose();
		WaitUntil(() => Process.GetProcessesByName("WpfPilot.ExampleApp").FirstOrDefault() == null);
		Assert.IsNull(Process.GetProcessesByName("WpfPilot.ExampleApp").FirstOrDefault());
	}

	[Test]
	public void TestPublicInterface()
	{
		using var appDriver = AppDriver.Launch(ExePath);

		// Test `GetElement` method.
		Assert.DoesNotThrow(() => appDriver.GetElement(x => x["Name"] == "EventDisplay"));
		Assert.Throws<TimeoutException>(() => appDriver.GetElement(x => x["Name"] == "NonExistentElement", timeoutMs: 250));

		// Test `GetElements` method.
		var listBoxItems = appDriver.GetElements(x => x["Name"].StartsWith("ListBoxItem"));
		Assert.AreEqual(3, listBoxItems.Count);
		listBoxItems.First().Assert(x => x["Name"] == "ListBoxItem1");
		listBoxItems.Last().Assert(x => x["Name"] == "ListBoxItem3");

		// Test valid `RunCode` method calls.
		var addIntResult = appDriver.RunCode(_ => ExampleStaticClass.AddInt(1, 2));
		Assert.AreEqual(3, addIntResult);

		var addNullableIntResult = appDriver.RunCode(_ => ExampleStaticClass.AddNullableInt(null, 2));
		Assert.AreEqual(2, addNullableIntResult);

		var addStringResult = appDriver.RunCode(_ => typeof(ExampleStaticClass).Invoke<string>("AddString", "Hello", "World"));
		Assert.AreEqual("HelloWorld", addStringResult);

		var supportsEvalExpressions = appDriver.RunCode(_ => typeof(ExampleStaticClass).Invoke<bool>("AddUserToDatabase", new ExampleStaticClass.User(), 123));
		Assert.IsTrue(supportsEvalExpressions);

		if (CurrentFramework.GetVersion() != "net452" && CurrentFramework.GetVersion() != "netcoreapp3.1") // This Invoke<dynamic> and Invoke<object> is only supported in .NET 5+
		{
			var dynamicThingResult = appDriver.RunCode(_ => typeof(ExampleStaticClass).Invoke<dynamic>("GetDynamicThing"));
			Assert.AreEqual("John", PropInfo.GetPropertyValue(dynamicThingResult, "FirstName"));
			Assert.AreEqual("Doe", PropInfo.GetPropertyValue(dynamicThingResult, "LastName"));
		}

		Assert.DoesNotThrow(() => appDriver.RunCode(_ => typeof(ExampleStaticClass).Invoke("UnserializableResult1")));
		Assert.DoesNotThrow(() => appDriver.RunCode(_ => typeof(ExampleStaticClass).Invoke("UnserializableResult2")));

		// Test `Screenshot` methods.
		AssertImage.IsValid(appDriver.Screenshot());
		AssertImage.IsValid(appDriver.Screenshot(ImageFormat.Jpeg));
		AssertImage.IsValid(appDriver.Screenshot(ImageFormat.Png));
		AssertImage.IsValid(appDriver.Screenshot(ImageFormat.Bmp));
		AssertImage.IsValid(appDriver.Screenshot(ImageFormat.Gif));

		appDriver.Screenshot($@"%TEMP%\{nameof(AppDriverTests)}.Screenshot.png");
		appDriver.Screenshot($@"%TEMP%\{nameof(AppDriverTests)}.Screenshot.jpg");
		appDriver.Screenshot($@"%TEMP%\{nameof(AppDriverTests)}.Screenshot.jpeg");
		appDriver.Screenshot($@"%TEMP%\{nameof(AppDriverTests)}.Screenshot.bmp");
		appDriver.Screenshot($@"%TEMP%\{nameof(AppDriverTests)}.Screenshot.gif");

		AssertImage.ExistsAndCleanup($@"%TEMP%\{nameof(AppDriverTests)}.Screenshot.png");
		AssertImage.ExistsAndCleanup($@"%TEMP%\{nameof(AppDriverTests)}.Screenshot.jpg");
		AssertImage.ExistsAndCleanup($@"%TEMP%\{nameof(AppDriverTests)}.Screenshot.jpeg");
		AssertImage.ExistsAndCleanup($@"%TEMP%\{nameof(AppDriverTests)}.Screenshot.bmp");
		AssertImage.ExistsAndCleanup($@"%TEMP%\{nameof(AppDriverTests)}.Screenshot.gif");

		// Test `Record` method.
		var recording = AppDriver.Record($@"%TEMP%\{nameof(AppDriverTests)}.Record.mp4");
		Task.Delay(5000).Wait(); // Let the recording run for 5 seconds.
		recording.Dispose();
		AssertImage.ExistsAndCleanup($@"%TEMP%\{nameof(AppDriverTests)}.Record.mp4");
	}

	private static void WaitUntil(Func<bool> condition, int timeoutMs = 10000)
	{
		var stopwatch = Stopwatch.StartNew();
		while (!condition() && stopwatch.ElapsedMilliseconds < timeoutMs)
			Task.Delay(100).GetAwaiter().GetResult();
	}

	public string ExePath { get; private set; } = "";
}
