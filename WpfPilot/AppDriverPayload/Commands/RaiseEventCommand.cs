namespace WpfPilot.AppDriverPayload.Commands;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using WpfPilot.AppDriverPayload;
using WpfPilot.Interop;
using WpfPilot.Utility;
using WpfPilot.Utility.WpfUtility;
using WpfPilot.Utility.WpfUtility.Helpers;
using WpfPilot.Utility.WpfUtility.SelectionHighlight;
using WpfPilot.Utility.WpfUtility.Tree;
using static WpfPilot.Interop.NamedPipeServer;

internal static class RaiseEventCommand
{
	public static void Process(Command command, TreeItem appRoot)
	{
		var targetIdString = PropInfo.GetPropertyValue(command.Value, "TargetId") ?? throw new ArgumentNullException("Missing TargetId property.");
		if (!Guid.TryParse(targetIdString, out Guid targetId))
			throw new ArgumentException($"Invalid TargetId `{targetIdString}`");

		var getRoutedEventArgs = PropInfo.GetPropertyValue(command.Value, "GetRoutedEventArgs") ?? throw new ArgumentNullException("Missing GetRoutedEventArgs property.");
		var target = TreeItem.GetTarget(appRoot, targetId) ?? throw new InvalidOperationException("Stale element. Cannot find the target element in the Visual Tree.");

		var targetUIElement = target as UIElement;
		if (targetUIElement is null)
			return;

		// Build the arg list.
		getRoutedEventArgs = ArgsMapper.MapSingle(getRoutedEventArgs);

		// Invoke unwrapped `Eval` function.
		RoutedEventArgs? routedEventArgs = getRoutedEventArgs.DynamicInvoke(target) as RoutedEventArgs;
		if (routedEventArgs is null)
			throw new InvalidOperationException($"Invalid type `{routedEventArgs?.GetType()}` does not inherit from RoutedEventArgs");

		var methods = ReflectionUtility.GetCandidateMethods(target.GetType(), "RaiseTrustedEvent", InvokeCommandBindings, new[] { routedEventArgs });
		var raiseTrustedEvent = ReflectionUtility.FindAndInvokeBestMatch;
		UIHighlight.Select(targetUIElement);

		using var reset = new ScopeGuard(exitAction: ControlHooks.ResetMouseState);

		// Fake mouse state, since devs will typically access and use the mouse device in the event handler.
		if (routedEventArgs.RoutedEvent.Name.Contains("MouseLeft") ||
			routedEventArgs.RoutedEvent.Name == "Click" ||
			routedEventArgs.RoutedEvent.Name == "MouseDown" ||
			routedEventArgs.RoutedEvent.Name == "MouseDoubleClick")
		{
			ControlHooks.IsLeftMousePressed = true;
		}
		else if (routedEventArgs.RoutedEvent.Name.Contains("MouseRight"))
		{
			ControlHooks.IsRightMousePressed = true;
		}

		var targets = GetAscendingVisualTree(targetUIElement);

		if (IsMouseEvent(routedEventArgs))
		{
			ControlHooks.MouseOverElement.SetValue(InputManager.Current.PrimaryMouseDevice, target);
			foreach (var element in targets)
				ControlHooks.WriteElementOverElement.Invoke(element, new object[] { ControlHooks.CoreFlags.IsMouseOverCache, true });
		}

		if (routedEventArgs.RoutedEvent.RoutingStrategy == RoutingStrategy.Direct)
		{
			// It is not uncommon a developer will try to invoke a direct event on the wrong element.
			// For example, raising a mouse click event on a text box within a button, when they intended the button.
			// We help them out here by interpretting the most appropriate match.
			var bestMatch = targets.FirstOrDefault(x => HasEventHandler(x, routedEventArgs.RoutedEvent.Name));
			if (bestMatch is null)
				raiseTrustedEvent(methods, target, new[] { routedEventArgs });
			else
				raiseTrustedEvent(methods, bestMatch, new[] { routedEventArgs });
		}
		else
		{
			raiseTrustedEvent(methods, target, new[] { routedEventArgs });
		}

		command.Respond(new { Success = true });
	}

	private static bool IsMouseEvent(RoutedEventArgs args) =>
		args.RoutedEvent.Name.Contains("Mouse") || args.RoutedEvent.Name == "Click";

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

	private static bool HasEventHandler(UIElement element, string eventName)
	{
		var events = element.GetType().GetEvents(InvokeCommandBindings).Where(x => x.Name == eventName);

		foreach (var theEvent in events)
		{
			var fieldInfo = element.GetType().GetField($"{theEvent.Name}Event", InvokeCommandBindings);
			RoutedEvent? eventKind = (RoutedEvent?) fieldInfo?.GetValue(element);
			if (eventKind is null)
				continue;

			// Look at the global event handlers store for any handlers.
			var store = typeof(UIElement)
				.GetField("EventHandlersStoreField", InvokeCommandBindings)
				?.GetValue(null); // `UncommonField<EventHandlersStore> EventHandlersStoreField`
			var method = store?.GetType().GetMethod("GetValue");
			var eventHandlersStore = method?.Invoke(store, new object[] { element });
			var getRoutedEventHandlers = eventHandlersStore?.GetType().GetMethod("GetRoutedEventHandlers");
			var handlers = (RoutedEventHandlerInfo[]?) getRoutedEventHandlers?.Invoke(eventHandlersStore, new[] { eventKind });

			if (handlers?.Any() == true)
				return true;
		}

		return false;
	}

	public const BindingFlags InvokeCommandBindings = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy;
}
