namespace WpfPilot.Tests;

using System.IO;
using System.Windows.Input;
using NUnit.Framework;
using WpfPilot.Tests.TestUtility;

[TestFixture]
public sealed class KeyboardTests : AppTestBase
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
	public void TestPublicInterface()
	{
		using var appDriver = AppDriver.Launch(ExePath);
		var eventDisplay = appDriver.GetElement(x => x["Name"] == "EventDisplay");

		appDriver.Keyboard.Press(Key.LeftCtrl, Key.A);
		Assert.AreEqual("Ctrl+A shortcut triggered.", eventDisplay["Text"]);

		var textbox = appDriver.GetElement(x => x["Name"] == "TextBox1");
		textbox["Text"] = "";
		textbox.Focus();

		appDriver.Keyboard.Type("This is a quick typing test. ");
		Assert.AreEqual("This is a quick typing test. ", textbox["Text"]);

		appDriver.Keyboard.Press(Key.LeftShift, Key.H, Key.I);
		Assert.AreEqual("This is a quick typing test. HI", textbox["Text"]);
	}

	public string ExePath { get; private set; } = "";
}
