namespace WpfPilot.Utility.WpfUtility.Tree;

using System.Windows;
using System.Windows.Controls.Primitives;

internal class PopupTreeItem : DependencyObjectTreeItem
{
	public PopupTreeItem(Popup target, TreeItem? parent, TreeService treeService)
		: base(target, parent, treeService)
	{
		this.TypedTarget = target;
	}

	public Popup TypedTarget { get; }

	protected override void ReloadCore()
	{
		base.ReloadCore();

		foreach (var child in LogicalTreeHelper.GetChildren(this.TypedTarget))
		{
			if (child is null)
			{
				continue;
			}

			this.AddChild(this.TreeService.Construct(child, this));
		}
	}
}