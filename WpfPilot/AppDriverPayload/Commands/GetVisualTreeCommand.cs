namespace WpfPilot.AppDriverPayload.Commands;

using System;
using WpfPilot.Utility.WpfUtility.Tree;
using static WpfPilot.Interop.NamedPipeServer;

internal static class GetVisualTreeCommand
{
	public static void Process(Command command, TreeService treeService)
	{
		var nodes = treeService.AllNodes;
		if (nodes.Count == 0)
			throw new InvalidOperationException("Unexpected TreeService state.");

		command.Respond(nodes);
	}
}