namespace WpfPilot.Utility.WpfUtility.Tree;

using System.Windows.Controls;

internal class ImageTreeItem : DependencyObjectTreeItem
{
	public ImageTreeItem(Image target, TreeItem? parent, TreeService treeService)
		: base(target, parent, treeService)
	{
		this.TypedTarget = target;
	}

	public Image TypedTarget { get; }

	protected override void ReloadCore()
	{
		base.ReloadCore();

		if (this.TypedTarget.Source is not null)
		{
			this.AddChild(this.TreeService.Construct(this.TypedTarget.Source, this));
		}
	}
}