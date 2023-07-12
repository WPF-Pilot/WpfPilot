<img align="left" width="80" height="80" src="logo.png" alt="WPF Pilot logo">

# WPF Pilot

Next gen automation testing for WPF apps.

```powershell
PM > Install-Package WpfPilot
```

## 📚 About

WPF Pilot is a NuGet Package for writing WPF automation tests. It works out of box with no special setup or configuration. WPF Pilot launches your app and injects a DLL to setup a communication channel between your test suite and app. It is built around the same technology as [Snoop](https://github.com/snoopwpf/snoopwpf) and does not use the <ins title="Microsoft UI Automation Framework">UIA</ins> in any way. In addition to automating WPF apps, WPF Pilot supports recording videos and taking screenshots of the app under test.

WPF Pilot's core is oriented around the **Visual Tree** and works incredibly well with [Snoop](https://github.com/snoopwpf/snoopwpf), which can be used to inspect the Visual Tree and find elements of interest to write tests around. Snoop is not affiliated with WPF Pilot, but is an amazing application you should consider [supporting](https://github.com/sponsors/batzen).

WPF Pilot works with _any_ test suite: NUnit, MSTest, XUnit, etc.

Guiding principles of WPF Pilot include:

- Just works. No special configuration or setup should be necessary. Install the package and get to hacking.
- Minimal yet expressive API. Everything you need and nothing more.

## 🔧 Requirements

WPF Pilot works on Windows and requires .NET Framework 4.5.2+, .NET Core 3.1+, or .NET 5+.

## 💗 API

There are only **4** core classes to learn, with a small handful of methods on each class. WPF Pilot strives to keep the API surface minimal, and encourages you to write your own helper methods for your specific domain. For brevity, not every method or class is documented below.

See the [docs](https://wpfpilot.dev/docs/tutorial) for detailed usage, tutorials, and comprehensive info.

**AppDriver**

The `AppDriver` is responsible for launching or attaching to an app and finding elements to interact with.

- `AppDriver.Launch` or `AppDriver.AttachTo`
- `AppDriver.GetElement(s)`
- `AppDriver.Screenshot`
- `AppDriver.Record`
- `AppDriver.RunCode`
- `AppDriver.Keyboard` (property)

**Element**

An `Element` represents a WPF `UIElement` in the app. It can be interacted with in all the expected ways.

- `Element.Click`, `Element.DoubleClick`, …
- `Element.Focus`
- `Element.Select`
- `Element.Check` / `Element.Uncheck`
- `Element.Expand` / `Element.Collapse`
- `Element.Type`
- `Element.Screenshot`  
[…]

Most `Element` methods are syntactic sugar around 3 core methods,

1. `Element.Invoke`
1. `Element.RaiseEvent`
1. `Element.SetProperty`

Which allow arbitrary code execution on the underlying element.

**Primitive**

Each property on an `Element` returns a `Primitive`. A `Primitive` represents any object, but is typically a string, number, `DateTime`, or the like. A `Primitive` comes with many support methods to work as expected. A primitive can be cast to its underlying type using `To<T>`, but this is generally unnecessary.

```csharp
var fullHealth = healthBarElement["Width"] / healthBarElement["FullWidth"];
Assert.AreEqual(0.8, fullHealth);

// Hide the health bar.
healthBarElement["Visibility"] = Visibility.Hidden.ToPrimitive();

// Check player name.
Assert.True(playerHUD["PlayerName"].StartsWith("Erik"));
```

**Keyboard**

`Keyboard` allows direct input to the app using a virtual keyboard.

- `Keyboard.Press`
- `Keyboard.Type`
- `Keyboard.Hotkey`

## 🛣️ Roadmap

- Low code recorder tool. Generate tests automatically by clicking through the app.
- Streamlined CEF and WebView2 support.
- Support for Linux and Mac.
- Low level input mocking.
- Minor API improvements.

## ✏️ Full Sample

```csharp
using WpfPilot;
using NUnit.Framework;

[TestFixture]
public class UserProfileTests
{
    [Test, Retry(1)]
    public void TestNewUserWorkflow()
    {
        using var appDriver = AppDriver.Launch("../bin/Debug/MyCoolApp.exe");

        // Start a screen recording if this is a retry run.
        using var recording = TestContext.CurrentContext.CurrentRepeatCount > 0 ?
            AppDriver.Record($"./TestRecordings/{nameof(TestNewUserWorkflow)}.mp4") : null;

        // Create a user.
        appDriver.GetElement(x => x["Name"] == "UsernameInput")
            .Type("CoolUser123")
            .Assert(x => x["Border"] == "Green");
        appDriver.GetElement(x => x["Name"] == "PasswordInput")
            .Type("myp@ssword1")
            .Assert(x => x["Border"] == "Green")
        appDriver.GetElement(x => x["Name"] == "SubmitForm").Click();

        // Accept cookies.
        var cookieBanner = appDriver.GetElement(x => x["Name"] == "CookieBanner");
        appDriver.GetElement(x => x["Name"] == "AcceptCookiesButton")
            .Assert(_ => cookieBanner["Visibility"].To<Visibility>() == Visibility.Hidden);

        // Check some interests.
        appDriver.GetElements(x => x["Name"].StartsWith("InterestOption"))
            .ForEach(x => x.Check());
        appDriver.GetElement(x => x["Text"] == "Finished").Click();

        // Verify we're on the news feed.
        appDriver.GetElement(x => x["Name"] == "InterestSection")
            .Assert(x => x.Child.Count != 0);
    }
}
```

## 📝 License

The WPF Pilot split license is viewable [here](LICENSE.txt). For most consumers this is the Apache License, Version 2.0. For larger corporations, an enterprise license is required. This funds continual maintenance, features, and ticket support. It can be purchased [here](https://wpfpilot.dev/pricing).

## 🧪 Troubleshooting

- Debug logs are stored at `%TEMP%/WpfPilot`
- If you cloned the project, and built in debug locally, and `AppDriverPayload` is injected into an exe, you can attach Visual Studio to the process and walk through the `AppDriverPayload` code like usual.
- `WpfPilot` uses DLL injection to setup a communication bridge. If you have an aggressive anti-virus, you may need to disable it. I have never personally encountered this, but it is worth mentioning.
- If the app is launched in Administrator mode, and the test suite is not, it may not have permissions to inject.