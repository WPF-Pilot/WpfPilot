﻿namespace WpfPilot.Utility.WpfUtility.Tree;

using System.Windows;

internal class WindowTreeItem : DependencyObjectTreeItem
{
	public WindowTreeItem(Window target, TreeItem? parent, TreeService treeService)
		: base(target, parent, treeService)
	{
		this.WindowTarget = target;
	}

	public Window WindowTarget { get; }

	protected override void ReloadCore()
	{
		foreach (Window? ownedWindow in this.WindowTarget.OwnedWindows)
		{
			if (ownedWindow is null)
			{
				continue;
			}

			if (ownedWindow.IsInitialized == false || ownedWindow.CheckAccess() == false)
			{
				continue;
			}

			var childWindowsTreeItem = new ChildWindowsTreeItem(this.WindowTarget, this, this.TreeService);
			childWindowsTreeItem.Reload();
			this.AddChild(childWindowsTreeItem);
			break;
		}

		base.ReloadCore();
	}
}