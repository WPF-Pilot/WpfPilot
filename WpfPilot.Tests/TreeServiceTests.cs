namespace WpfPilot.Tests;

using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Newtonsoft.Json;
using NUnit.Framework;
using WpfPilot.Utility.WpfUtility.Tree;

[TestFixture]
public sealed class TreeServiceTests
{
	[Test]
	public void TestTreeSerialization()
	{
		using var treeService = new TreeService(new HashSet<string>() { "Text", "Margin", "Padding", "Content", "Foreground", "FontWeight" });
		var target = new StackPanel();
		target.Children.Add(new TextBlock { Text = "test" });
		target.Children.Add(new Border { Child = new CheckBox { Content = "check" }, Margin = new Thickness(1), Padding = new Thickness(2) });
		target.Children.Add(new Button
		{
			Content = "click",
			Foreground = new SolidColorBrush(Color.FromArgb(0, 1, 2, 3)),
			FontWeight = FontWeight.FromOpenTypeWeight(10),
		});
		treeService.Construct(target, null);

		var result = treeService.AllNodes;
		var serialized = JsonConvert.SerializeObject(result);

		Assert.IsNotNull(serialized);
		Assert.AreNotEqual(0, result.Count);
	}
}
