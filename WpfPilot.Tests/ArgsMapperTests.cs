namespace WpfPilot.Tests;

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using NUnit.Framework;
using WpfPilot.ExampleApp;
using WpfPilot.Interop;

[TestFixture]
public sealed class ArgsMapperTests
{
	[Test]
	public void TestArgMapperSuite()
	{
		Assert.DoesNotThrow(() =>
		{
			ArgsMapper.MapSingle(Serialize(Eval.SerializeCode(() => new RoutedEventArgs(ButtonBase.ClickEvent))));
			ArgsMapper.MapSingle(Serialize(Eval.SerializeCode(() => ExampleStaticClass.AddNullableInt(null, 2))));
			ArgsMapper.MapSingle(Serialize(Eval.SerializeCode(() => Noop())));
			ArgsMapper.MapSingle(Serialize(Eval.SerializeCode(() => typeof(ExampleStaticClass).Invoke<bool>("AddUserToDatabase", new ExampleStaticClass.User(), 123))));
			ArgsMapper.MapSingle(Serialize(Eval.SerializeCode(() => typeof(ExampleStaticClass).Invoke<string>("AddString", "Hello", "World"))));
			ArgsMapper.MapSingle(Serialize(Eval.SerializeCode(() => Math.Sign(1) == 0 ? 333 : 555)));
		});
	}

	[Test]
	public void HandlesExoticTypes()
	{
		var tuple = Serialize(Eval.SerializeCode(() => new Tuple<int, int, int>(1, 2, 3)));
		var tupleWithList = Serialize(Eval.SerializeCode(() => new Tuple<List<int>, int, int>(new List<int>(), 1, 2)));
		var memoryStream = Serialize(Eval.SerializeCode(() => new MemoryStream()));
		var solidBrush = Serialize(Eval.SerializeCode(() => new SolidColorBrush(Colors.Red)));
		var gradientBrush = Serialize(Eval.SerializeCode(() => new LinearGradientBrush(Colors.Red, Colors.Blue, 0)));
		var style = Serialize(Eval.SerializeCode(() => new Style()));

		Assert.True(ArgsMapper.MapSingle(tuple) is Func<Tuple<int, int, int>>);
		Assert.True(ArgsMapper.MapSingle(tupleWithList) is Func<Tuple<List<int>, int, int>>);
		Assert.True(ArgsMapper.MapSingle(memoryStream) is Func<MemoryStream>);
		Assert.True(ArgsMapper.MapSingle(solidBrush) is Func<SolidColorBrush>);
		Assert.True(ArgsMapper.MapSingle(gradientBrush) is Func<LinearGradientBrush>);
		Assert.True(ArgsMapper.MapSingle(style) is Func<Style>);
	}

	[Test]
	public void HandlesLocalVariables()
	{
		var coolUsername = "erik.lanning@logos.com";
		var textComposition = Serialize(Eval.SerializeCode(() => new TextCompositionEventArgs(InputManager.Current.PrimaryKeyboardDevice, new TextComposition(InputManager.Current, null, coolUsername))
		{
			RoutedEvent = UIElement.TextInputEvent,
		}));

		Assert.DoesNotThrow(() =>
		{
			ArgsMapper.MapSingle(textComposition);
		});
	}

	[Test]
	public void ThrowsOnWait()
	{
		Assert.Throws<InvalidOperationException>(() =>
		{
			Serialize(Eval.SerializeCode(() => SampleAsync().GetAwaiter()));
		});

		Assert.Throws<InvalidOperationException>(() =>
		{
			Serialize(Eval.SerializeCode(() => SampleAsync().Wait()));
		});

		Assert.Throws<InvalidOperationException>(() =>
		{
			Serialize(Eval.SerializeCode(() => SampleAsync().Result));
		});
	}

	public static void Noop()
	{
	}

	public static async Task<int> SampleAsync()
	{
		await Task.Delay(100);
		return 0;
	}

	private static dynamic? Serialize(dynamic? obj)
	{
		var serialized = MessagePacker.Pack(obj);
		var deserialized = MessagePacker.Unpack(serialized);
		return deserialized;
	}
}
