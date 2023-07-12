namespace WpfPilot.Tests.Mocks;

using System;

internal class Animal
{
	public static void StaticMethod()
	{
		Console.WriteLine("Static method");
	}

	public void Name()
	{
		Console.WriteLine("TODO.");
	}

	public virtual void Eat()
	{
		Console.WriteLine("I'm eating");
	}

	private void SecretAnimalMethod()
	{
		Console.WriteLine("Shhh.");
	}
}
