namespace WpfPilot.AppDriverPayload;

using System.Windows;
using System.Windows.Input;

internal static class KeyboardInput
{
	public static void Type(string text)
	{
		var textCompositionEvent = new TextCompositionEventArgs(
			InputManager.Current.PrimaryKeyboardDevice,
			new TextComposition(InputManager.Current, InputManager.Current.PrimaryKeyboardDevice.FocusedElement, text))
		{
			RoutedEvent = UIElement.PreviewTextInputEvent,
			Source = InputManager.Current.PrimaryKeyboardDevice.FocusedElement,
		};
		InputManager.Current.ProcessInput(textCompositionEvent);
	}
}
