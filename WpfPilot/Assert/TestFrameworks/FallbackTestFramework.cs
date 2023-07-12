namespace WpfPilot.Assert.TestFrameworks;

internal class FallbackTestFramework : ITestFramework
{
	public bool IsAvailable => true;

	public void Throw(string message)
	{
		throw new AssertionFailedException(message);
	}
}
