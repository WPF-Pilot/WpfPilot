namespace WpfPilot.Tests.Mocks;

using System;

internal class Bird : Animal
{
	public override void Eat()
	{
		Console.WriteLine("I'm eating seeds");
	}

	public void Fly()
	{
		Console.WriteLine("I'm flying");
	}

	protected void Cool()
	{
		Console.WriteLine("I'm cool");
	}

	private void LayEgg()
	{
		Console.WriteLine("I'm laying an egg - in Bird class.");
	}
}
