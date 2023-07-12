namespace WpfPilot.Tests;

using System.IO;
using NUnit.Framework;

[TestFixture]
public class AppTestBase
{
	[SetUp]
	public virtual void Setup()
	{
		// If you're consuming this library via a NuGet package, this setup is not necessary, but because this repo uses
		// project references, we must manually move the resource files.
		{
			var source = Path.Combine(TestContext.CurrentContext.TestDirectory, @"contentFiles\any\any\WpfPilotResources");
			var destination = Path.Combine(TestContext.CurrentContext.WorkDirectory, "WpfPilotResources");
			Directory.CreateDirectory(destination);
			foreach (var file in Directory.GetFiles(source))
			{
				var fileName = Path.GetFileName(file);
				var outputPath = Path.Combine(destination, fileName);
				if (!File.Exists(outputPath))
					File.Move(file, Path.Combine(destination, fileName));
			}
		}

		{
			var source = Path.Combine(TestContext.CurrentContext.TestDirectory, @"contentFiles\any\any\WpfPilotResources\x64");
			var destination = Path.Combine(TestContext.CurrentContext.WorkDirectory, @"WpfPilotResources\x64");
			Directory.CreateDirectory(destination);
			foreach (var file in Directory.GetFiles(source))
			{
				var fileName = Path.GetFileName(file);
				var outputPath = Path.Combine(destination, fileName);
				if (!File.Exists(outputPath))
					File.Move(file, outputPath);
			}
		}

		{
			var source = Path.Combine(TestContext.CurrentContext.TestDirectory, @"contentFiles\any\any\WpfPilotResources\x86");
			var destination = Path.Combine(TestContext.CurrentContext.WorkDirectory, @"WpfPilotResources\x86");
			Directory.CreateDirectory(destination);
			foreach (var file in Directory.GetFiles(source))
			{
				var fileName = Path.GetFileName(file);
				var outputPath = Path.Combine(destination, fileName);
				if (!File.Exists(outputPath))
					File.Move(file, outputPath);
			}
		}
	}
}
