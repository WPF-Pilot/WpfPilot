namespace WpfPilot.AppDriverPayload.Commands;

using System;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using WpfPilot.Interop;
using WpfPilot.Utility.WpfUtility;
using WpfPilot.Utility.WpfUtility.SelectionHighlight;
using WpfPilot.Utility.WpfUtility.Tree;
using static WpfPilot.Interop.NamedPipeServer;

internal static class InvokeCommand
{
	public static async Task ProcessAsync(Command command, TreeItem appRoot)
	{
		var targetIdString = PropInfo.GetPropertyValue(command.Value, "TargetId") ?? throw new ArgumentNullException("Missing TargetId property.");
		if (!Guid.TryParse(targetIdString, out Guid targetId))
			throw new ArgumentException($"Invalid TargetId `{targetIdString}`");

		var target = TreeItem.GetTarget(appRoot, targetId);
		if (target is null)
		{
			command.Respond(new { Value = "StaleElement" });
			return;
		}

		var code = PropInfo.GetPropertyValue(command.Value, "Code") ?? throw new ArgumentNullException("Missing Code property.");

		if (target is DependencyObject dp)
			UIHighlight.Select(dp);

		var func = (Delegate) ArgsMapper.MapSingle(code);

		object? result = null;
		if (func.Method.ReturnType.IsSubclassOf(typeof(Task)))
		{
			dynamic task = func.DynamicInvoke(target)!;
			result = await task;
		}
		else if (func.Method.ReturnType == typeof(Task))
		{
			dynamic task = func.DynamicInvoke(target)!;
			await task;
		}
		else
		{
			result = func.DynamicInvoke(target);
		}

		command.Respond(ArgsMapper.IsSerializable(result) ? WrappedArg<object>.Wrap(result) : new { Value = "UnserializableResult" });
	}

	public const BindingFlags InvokeCommandBindings = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy;
}
