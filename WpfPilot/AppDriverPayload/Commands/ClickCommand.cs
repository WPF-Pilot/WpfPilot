namespace WpfPilot.AppDriverPayload.Commands;

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using WpfPilot.AppDriverPayload;
using WpfPilot.Utility;
using WpfPilot.Utility.WpfUtility;
using WpfPilot.Utility.WpfUtility.Helpers;
using WpfPilot.Utility.WpfUtility.SelectionHighlight;
using WpfPilot.Utility.WpfUtility.Tree;
using static WpfPilot.Interop.NamedPipeServer;

internal static class ClickCommand
{
	public static void Process(Command command, TreeItem appRoot)
	{
		var targetIdString = PropInfo.GetPropertyValue(command.Value, "TargetId") ?? throw new ArgumentNullException("Missing TargetId property.");
		if (!Guid.TryParse(targetIdString, out Guid targetId))
			throw new ArgumentException($"Invalid TargetId `{targetIdString}`");

		var target = TreeItem.GetTarget(appRoot, targetId) ?? throw new InvalidOperationException("Stale element. Cannot find the target element in the Visual Tree.");
		var mouseButtonString = PropInfo.GetPropertyValue(command.Value, "MouseButton") ?? throw new ArgumentNullException("Missing MouseButton property.");
		if (mouseButtonString != "Right" && mouseButtonString != "Left")
			throw new ArgumentException($"Invalid MouseButton `{mouseButtonString}`. Expected `Left` or `Right`.");

		var targetUIElement = target as UIElement;

		// No-op seems better than an exception in this case. Possibly review this.
		if (targetUIElement is null)
			return;

		UIHighlight.Select(targetUIElement);
		using var reset = new ScopeGuard(exitAction: ControlHooks.ResetMouseState);

		var mouseButton = (MouseButton) Enum.Parse(typeof(MouseButton), mouseButtonString);
		var targets = GetAscendingVisualTree(targetUIElement);

		/**
		 * Do mouse down events.
		 */
		ControlHooks.SetButton(mouseButton, isPressed: true);
		DoClick(UIElement.PreviewMouseDownEvent, targetUIElement, mouseButton, targets);
		DoClick(UIElement.MouseDownEvent, targetUIElement, mouseButton, targets);

		/**
		 * Do mouse up events.
		 */
		ControlHooks.SetButton(mouseButton, isPressed: false);
		DoClick(UIElement.PreviewMouseUpEvent, targetUIElement, mouseButton, targets);
		DoClick(UIElement.MouseUpEvent, targetUIElement, mouseButton, targets);

		command.Respond(new { Success = true });
	}

	private static void DoClick(RoutedEvent routedEvent, UIElement source, MouseButton mouseButton, IReadOnlyList<UIElement> targets)
	{
		var args = new object[]
		{
			new MouseButtonEventArgs(Mouse.PrimaryDevice, Environment.TickCount, mouseButton)
			{
				RoutedEvent = routedEvent,
				Source = source,
			}
		};
		var methods = ReflectionUtility.GetCandidateMethods(typeof(UIElement), "RaiseTrustedEvent", InvokeCommandBindings, args);
		var raiseTrustedEvent = ReflectionUtility.FindAndInvokeBestMatch;

		ControlHooks.MouseOverElement.SetValue(Mouse.PrimaryDevice, source);
		foreach (var target in targets)
			ControlHooks.WriteElementOverElement.Invoke(target, new object[] { ControlHooks.CoreFlags.IsMouseOverCache, true });

		raiseTrustedEvent(methods, source, args);
	}

	private static IReadOnlyList<UIElement> GetAscendingVisualTree(UIElement element)
	{
		var targets = new List<UIElement>();
		while (element is not null)
		{
			targets.Add(element);
			element = (UIElement) (VisualTreeHelper.GetParent(element) ?? LogicalTreeHelper.GetParent(element));
		}

		return targets;
	}

	public const BindingFlags InvokeCommandBindings = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy;
}
