namespace WpfPilot.Tests;

using System;
using System.IO;
using System.Linq;
using System.Windows;
using NUnit.Framework;
using WpfPilot;
using WpfPilot.Tests.Elements;
using WpfPilot.Tests.TestUtility;
using WpfPilot.Utility;

[TestFixture]
public sealed class ElementTests : AppTestBase
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

		// Test button events.
		appDriver.GetElement(x => x["Name"] == "HelloWorldButton")
			.Click()
			.Assert(_ => eventDisplay["Text"] == "HelloWorldButton_Click event triggered.")
			.DoubleClick()
			.Assert(_ => eventDisplay["Text"] == "HelloWorldButton_DoubleClick event triggered.")
			.RightClick()
			.Assert(_ => eventDisplay["Text"] == "HelloWorldButton_RightClick event triggered.");

		// Test checkbox events.
		appDriver.GetElement(x => x["Uid"] == "MainCheckbox")
			.Uncheck()
			.Assert(_ => eventDisplay["Text"] == "MainCheckbox_Unchecked event triggered.")
			.Check()
			.Assert(_ => eventDisplay["Text"] == "MainCheckbox_Checked event triggered.");

		// Test text box events.
		appDriver.GetElement(x => x["Name"] == "TextBox1")
			.SetProperty("Text", "")
			.Focus()
			.Assert(_ => eventDisplay["Text"] == "TextBox1_GotKeyboardFocus event triggered.")
			.Type("Hello World!")
			.Assert(x => x["Text"] == "Hello World!")
			.SelectText("Hello")
			.Assert(_ => eventDisplay["Text"] == "TextBox1_SelectionChanged event triggered.");

		// Test selection list events.
		appDriver.GetElement(x => x["Name"] == "ListBoxItem2")
			.Select()
			.Assert(_ => eventDisplay["Text"] == "ListBoxItem2 selected event triggered.");

		// Test expander events.
		appDriver.GetElement(x => x["Name"] == "ExpanderControl")
			.Expand()
			.Assert(_ => eventDisplay["Text"] == "ExpanderControl_Expanded event triggered.")
			.Collapse()
			.Assert(_ => eventDisplay["Text"] == "ExpanderControl_Collapsed event triggered.");

		// Test menus.
		appDriver.GetElement(x => x["Header"] == "Menu Header").Click();
		appDriver.GetElement(x => x["Header"] == "MenuItemOne").Click().Assert(x => x["IsChecked"] == true);
		var menuItemOne = appDriver.GetElement(x => x["Header"] == "MenuItemOne");

		// Test context menus.
		appDriver.GetElement(x => x["Name"] == "HelloWorldButton")
			.RightClick();
		appDriver.GetElement(x => x["Name"] == "FileContextMenuItem")
			.Click()
			.Assert(_ => eventDisplay["Text"] == "HelloWorldContextMenuFile_Click event triggered.");

		// Test other windows.
		appDriver.GetElement(x => x["Content"] == "Open a message box").Click();
		appDriver.Keyboard.Press(System.Windows.Input.Key.Tab, System.Windows.Input.Key.Enter);
		appDriver.GetElement(x => x["Content"] == "Open other window").Click();
		appDriver.GetElement(x => x.TypeName == "CheckBox" && x["Content"] == "CoolCheckBox").Click().Assert(x => x["IsChecked"] == true);

		// Test ScrollIntoView.
		var scrollViewer = appDriver.GetElement<MyScrollViewer>(x => x["Name"] == "ScrollViewer");
		var initialOffset = scrollViewer["VerticalOffset"];
		appDriver.GetElement(x => x["Name"] == "ExpanderControl").Expand();
		appDriver.GetElement(x => x["Name"] == "SecondTextBlock")
			.ScrollIntoView()
			.Assert(_ => scrollViewer["VerticalOffset"] > initialOffset);

		// Test property methods.
		var checkbox = appDriver.GetElement(x => x["Uid"] == "MainCheckbox").Assert(x => x["ActualWidth"] > 10);
		Assert.True(checkbox.HasProperty("Visibility"));
		Assert.True(checkbox["Visibility"].To<Visibility>() == Visibility.Visible);

		checkbox["Visibility"] = Visibility.Hidden.ToPrimitive();
		checkbox["Margin"] = "5, 5, 5, 5";
		Assert.True(checkbox["Visibility"].To<Visibility>() == Visibility.Hidden);
		Assert.True(checkbox["Margin"] == "5,5,5,5");

		// Test properties.
		Assert.AreEqual("CheckBox", checkbox.TypeName);

		// Test child and parent selectors.
		Assert.NotNull(checkbox.Parent);
		checkbox.Parent!.Assert(x => x.Child.Any(y => y["Uid"] == checkbox["Uid"]));
		checkbox.Parent.Child.First(x => x["Name"] == checkbox["Name"]).SetProperty("Width", 123);
		Assert.AreEqual(123, checkbox["Width"]);

		// Test screenshot methods.
		checkbox.Screenshot(out var pngBytes, ImageFormat.Png);
		checkbox.Screenshot(out var jpgBytes, ImageFormat.Jpeg);
		checkbox.Screenshot(out var gifBytes, ImageFormat.Gif);
		checkbox.Screenshot(out var bmpBytes, ImageFormat.Bmp);

		AssertImage.IsValid(pngBytes);
		AssertImage.IsValid(jpgBytes);
		AssertImage.IsValid(gifBytes);
		AssertImage.IsValid(bmpBytes);

		checkbox.Screenshot("%TEMP%/checkbox.png");
		checkbox.Screenshot("./checkbox.jpg");

		AssertImage.ExistsAndCleanup("%TEMP%/checkbox.png");
		AssertImage.ExistsAndCleanup("./checkbox.jpg");
	}

	[TestCase(0)]
	[TestCase(1)]
	public void TestShutdown(int exitCode)
	{
		using var appDriver = AppDriver.Launch(ExePath);
		if (exitCode == 0)
			Assert.DoesNotThrow(() => appDriver.RunCode(app => app.Shutdown(exitCode)));
		else
			Assert.Throws<Retry.RetryException>(() => appDriver.RunCode(app => app.Shutdown(exitCode)));
	}

	[Test]
	public void TestUserClose()
	{
		using var appDriver = AppDriver.Launch(ExePath);
		Assert.DoesNotThrow(() => appDriver.RunCode(app => app.MainWindow.Close()));
	}

	[Test]
	public void TestExceptionClose()
	{
		using var appDriver = AppDriver.Launch(ExePath);
		Assert.Throws<InvalidOperationException>(() => appDriver.GetElement(x => x["Name"] == "ThrowExceptionButton").Click());
	}

	private string ExePath { get; set; } = "";
}
