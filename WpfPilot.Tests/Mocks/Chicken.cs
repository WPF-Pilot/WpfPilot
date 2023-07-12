#pragma warning disable

namespace WpfPilot.Tests.Mocks;

using System;

internal class Chicken : Bird
{
	public override void Eat()
	{
		Console.WriteLine("I'm eating corn");
	}

	public void LayEgg()
	{
		Console.WriteLine("I'm laying an egg");
	}

	protected void Cluck()
	{
		Console.WriteLine("Cluck");
	}

	private void NullParamsCheck(string? a, string? b)
	{
		Console.WriteLine("NullParamsCheck");
	}
}
