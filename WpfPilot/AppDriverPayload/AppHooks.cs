#pragma warning disable

namespace WpfPilot.AppDriverPayload;

using System;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Controls.Ribbon;
using System.Windows.Input;
using HarmonyLib;
using WpfPilot.Utility;

// Many WPF controls use the mouse coordinates to determine if an element has been interacted with.
// Because we cannot fake the mouse coordinates, we need to workaround the limitation by patching various controls to fake mouse positioning.
// Many techniques on patching the mouse coordinates themself do not work, because the code is security critical, so we're left with this method.
internal static class AppHooks
{
	static AppHooks()
	{
		// Warmup hooked functions so the IL instructions are in memory.
		// Otherwise we can occasionally run into "empty body" exceptions.
		_ = Mouse.PrimaryDevice.LeftButton;

		var button = new Button();
		typeof(ButtonBase).InvokeOn(button, "UpdateIsPressed");

		var gridColumnHeader = new GridViewColumnHeader();
		typeof(GridViewColumnHeader).InvokeOn(gridColumnHeader, "IsMouseOutside");

		var menuItem = new MenuItem();
		typeof(MenuItem).InvokeOn(menuItem, "HandleMouseDown", new MouseButtonEventArgs(Mouse.PrimaryDevice, 0, MouseButton.Left)
		{
			RoutedEvent = MenuItem.MouseDownEvent,
		});
		typeof(MenuItem).InvokeOn(menuItem, "HandleMouseUp", new MouseButtonEventArgs(Mouse.PrimaryDevice, 0, MouseButton.Left)
		{
			RoutedEvent = MenuItem.MouseUpEvent,
		});
		typeof(MenuItem).InvokeOn(menuItem, "UpdateIsPressed");

		var ribbonMenuItem = new RibbonMenuItem();
		typeof(RibbonMenuItem).InvokeOn(ribbonMenuItem, "UpdateIsPressed");

		var checkBox = new CheckBox();
		typeof(DataGridCheckBoxColumn).Invoke("IsMouseOver", checkBox, new MouseButtonEventArgs(Mouse.PrimaryDevice, 0, MouseButton.Left)
		{
			RoutedEvent = MenuItem.MouseUpEvent,
		});

		var harmony = new Harmony("com.wpfpilot.apphooks.patch");
		harmony.PatchAll(Assembly.GetExecutingAssembly());
	}

	public static void EnsureHooked()
	{
		// Ensures static constructor is called.
	}

	[HarmonyPatch(typeof(MouseDevice), "GetButtonState")]
	public static class PatchMouseDeviceGetButtonState
	{
		public static bool Prefix(ref MouseButtonState __result, MouseButton mouseButton)
		{
			if (mouseButton == MouseButton.Right && IsRightMousePressed != null)
			{
				__result = IsRightMousePressed == true ? MouseButtonState.Pressed : MouseButtonState.Released;
				return false;
			}

			if (mouseButton == MouseButton.Left && IsLeftMousePressed != null)
			{
				__result = IsLeftMousePressed == true ? MouseButtonState.Pressed : MouseButtonState.Released;
				return false;
			}

			// Let the original method run.
			return true;
		}
	}

	[HarmonyPatch(typeof(ButtonBase), "UpdateIsPressed")]
	public static class PatchButtonUpdateIsPressed
	{
		public static bool Prefix(ButtonBase __instance)
		{
			var isPressed = (bool) typeof(ButtonBase).GetProperty("IsPressed", Flags).GetValue(__instance);
			var methods = ReflectionUtility.GetCandidateMethods(typeof(ButtonBase), "SetIsPressed", Flags, new object[] { true });

			if (!isPressed)
				ReflectionUtility.FindAndInvokeBestMatch(methods, __instance, new object[] { true });
			else if (isPressed)
				ReflectionUtility.FindAndInvokeBestMatch(methods, __instance, new object[] { false });

			return false;
		}
	}

	[HarmonyPatch(typeof(GridViewColumnHeader), "IsMouseOutside")]
	public static class PatchGridViewColumnHeaderIsMouseOutside
	{
		public static bool Prefix(ref bool __result)
		{
			// Change `IsMouseOutside` to return false, meaning the mouse is inside the control.
			__result = false;
			return false;
		}
	}

	[HarmonyPatch(typeof(MenuItem), "HandleMouseDown")]
	public static class PatchMenuItemHandleMouseDown
	{
		public static bool Prefix(MenuItem __instance, object[] __args)
		{
			var e = (MouseButtonEventArgs) __args[0];

			// Click happens on down for headers
			MenuItemRole role = (MenuItemRole) typeof(MenuItem).GetProperty("Role", Flags).GetValue(__instance);

			if (role == MenuItemRole.TopLevelHeader || role == MenuItemRole.SubmenuHeader)
			{
				var methods = ReflectionUtility.GetCandidateMethods(typeof(MenuItem), "ClickHeader", Flags, null);
				ReflectionUtility.FindAndInvokeBestMatch(methods, __instance, null);
			}

			// Handle mouse messages b/c they were over me, I just didn't use it
			e.Handled = true;
			return false;
		}
	}

	[HarmonyPatch(typeof(MenuItem), "HandleMouseUp")]
	public static class PatchMenuItemHandleMouseUp
	{
		public static bool Prefix(MenuItem __instance, object[] __args)
		{
			var e = (MouseButtonEventArgs) __args[0];

			// Click happens on up for items
			MenuItemRole role = (MenuItemRole) typeof(MenuItem).GetProperty("Role", Flags).GetValue(__instance);

			if (role == MenuItemRole.TopLevelItem || role == MenuItemRole.SubmenuItem)
			{
				var methods = ReflectionUtility.GetCandidateMethods(typeof(MenuItem), "ClickItem", Flags, new object[] { true });
				ReflectionUtility.FindAndInvokeBestMatch(methods, __instance, new object[] { true });
			}

			if (e.ChangedButton != MouseButton.Right)
			{
				// Handle all clicks unless there's a possibility of a ContextMenu inside a Menu.
				e.Handled = true;
			}

			return false;
		}
	}

	[HarmonyPatch(typeof(MenuItem), "UpdateIsPressed")]
	public static class PatchMenuItemUpdateIsPressed
	{
		public static bool Prefix(MenuItem __instance)
		{

			if (Mouse.LeftButton == MouseButtonState.Pressed)
			{
				typeof(MenuItem).GetProperty("IsPressed", Flags).SetValue(__instance, true);
			}
			else
			{
				var isPressedValue = (DependencyPropertyKey) typeof(MenuItem)
					.GetField("IsPressedPropertyKey", BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.FlattenHierarchy)
					.GetValue(null);
				var args = new object[] { isPressedValue };
				var methods = ReflectionUtility.GetCandidateMethods(typeof(MenuItem), "ClearValue", Flags, args);
				ReflectionUtility.FindAndInvokeBestMatch(methods, __instance, args);
			}

			return false;
		}
	}

	[HarmonyPatch(typeof(RibbonMenuItem), "UpdateIsPressed")]
	public static class PatchRibbonMenuItemUpdateIsPressed
	{
		public static bool Prefix(RibbonMenuItem __instance)
		{
			if (Mouse.LeftButton == MouseButtonState.Pressed)
				typeof(RibbonMenuItem).GetProperty("IsPressed", Flags).SetValue(__instance, true);
			else
				typeof(RibbonMenuItem).GetProperty("IsPressed", Flags).SetValue(__instance, false);

			return false;
		}
	}

	[HarmonyPatch(typeof(DataGridCheckBoxColumn), "IsMouseOver")]
	public static class PatchDataGridCheckBoxColumnIsMouseOver
	{
		public static bool Prefix(ref bool __result)
		{
			__result = true;
			return false;
		}
	}

	[HarmonyPatch(typeof(Window), "ShowDialog")]
	public static class PatchWindowShowDialog
	{
		// We need a way to know if `ShowDialog` was called, to avoid blocking.
		public static bool Prefix(Window __instance)
		{
			AppHooks.ShowDialogCalled = true;
			return true;
		}
	}

	public static void SetButton(MouseButton mouseButton, bool isPressed)
	{
		if (mouseButton == MouseButton.Left)
			AppHooks.IsLeftMousePressed = isPressed;
		else if (mouseButton == MouseButton.Right)
			AppHooks.IsRightMousePressed = isPressed;
	}

	public static void ResetMouseState()
	{
		IsLeftMousePressed = null;
		IsRightMousePressed = null;
	}

	public static bool ShowDialogCalled
	{
		get => _showDialogCalled;
		set => _showDialogCalled = value;
	}

	public static bool? IsLeftMousePressed = null;
	public static bool? IsRightMousePressed = null;
	public static FieldInfo MouseOverElement = typeof(MouseDevice).GetField("_mouseOver", BindingFlags.NonPublic | BindingFlags.Instance);
	public static MethodInfo WriteElementOverElement = typeof(UIElement).GetMethod("WriteFlag", BindingFlags.NonPublic | BindingFlags.Instance);

	[Flags] // Copied from WPF UIElement.
	public enum CoreFlags : uint
	{
		IsMouseOverCache = 0x00001000,
	}

	private static volatile bool _showDialogCalled = false;
	private const BindingFlags Flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.FlattenHierarchy | BindingFlags.Static;
}
