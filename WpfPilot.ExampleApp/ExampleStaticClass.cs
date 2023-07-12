namespace WpfPilot.ExampleApp;

using System.IO;

public static class ExampleStaticClass
{
	public class User
	{
		public string FirstName { get; set; } = "";
		public string LastName { get; set; } = "";
	}

	public static int AddInt(int a, int b)
	{
		return a + b;
	}

	public static int AddNullableInt(int? a, int? b)
	{
		return (a ?? 0) + (b ?? 0);
	}

	internal static string AddString(string a, string b)
	{
		return a + b;
	}

	private static bool AddUserToDatabase(User user, int timestamp)
	{
		// Do cool stuff with the user object here.
		// ...
		return true;
	}

	private static dynamic GetDynamicThing()
	{
		return new
		{
			FirstName = "John",
			LastName = "Doe"
		};
	}

	private static void UnserializableResult1()
	{
	}

	private static Stream UnserializableResult2()
	{
		return new MemoryStream();
	}
}
