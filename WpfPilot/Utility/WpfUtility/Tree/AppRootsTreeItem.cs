namespace WpfPilot.Utility.WpfUtility.Tree;

using System.Windows;
using System.Windows.Media;

internal class AppRootsTreeItem : TreeItem
{
	public AppRootsTreeItem(AppRoots appRoots, TreeItem? parent, TreeService treeService)
	: base(appRoots.App, parent, treeService)
	{
		this.appRoots = appRoots;
	}

	protected override void ReloadCore()
	{
		base.ReloadCore();

		foreach (Window? window in this.appRoots.App.Windows)
		{
			if (window is null)
			{
				continue;
			}

			if (window.IsInitialized == false || window.CheckAccess() == false)
			{
				continue;
			}

			// windows which have an owner are added as child items in VisualItem, so we have to skip them here
			if (window.Owner is not null)
			{
				continue;
			}

			this.AddChild(this.TreeService.Construct(window, this));
		}

		foreach (Visual visual in this.appRoots.Visuals)
		{
			if (visual is UIElement uiElement)
				this.AddChild(this.TreeService.Construct(uiElement, this));
		}
	}

	private readonly AppRoots appRoots;
}
