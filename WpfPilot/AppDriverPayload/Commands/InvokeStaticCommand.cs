namespace WpfPilot.AppDriverPayload.Commands;

using System;
using System.Threading.Tasks;
using System.Windows;
using WpfPilot.Interop;
using WpfPilot.Utility.WpfUtility;
using static WpfPilot.Interop.NamedPipeServer;

internal static class InvokeStaticCommand
{
	public static async Task ProcessAsync(Command command)
	{
		var code = PropInfo.GetPropertyValue(command.Value, "Code") ?? throw new ArgumentNullException("Missing Code property.");

		var func = (Delegate) ArgsMapper.MapSingle(code);

		object? result = null;
		if (func.Method.ReturnType.IsSubclassOf(typeof(Task)))
		{
			dynamic task = func.DynamicInvoke(Application.Current)!;
			result = await task;
		}
		else if (func.Method.ReturnType == typeof(Task))
		{
			dynamic task = func.DynamicInvoke(Application.Current)!;
			await task;
		}
		else
		{
			result = func.DynamicInvoke(Application.Current);
		}

		command.Respond(ArgsMapper.IsSerializable(result) ? WrappedArg<object>.Wrap(result) : new { Value = "UnserializableResult" });
	}
}
