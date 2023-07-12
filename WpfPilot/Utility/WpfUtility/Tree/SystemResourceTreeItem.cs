namespace WpfPilot.Utility.WpfUtility.Tree;

using System.Reflection;
using System.Windows;

internal sealed class SystemResourceTreeItem : ResourceDictionaryTreeItem
{
	private readonly Assembly assembly;

	public SystemResourceTreeItem(Assembly assembly, ResourceDictionary dictionary, TreeItem? parent, TreeService treeService)
		: base(dictionary, parent, treeService)
	{
		this.assembly = assembly;
	}

	protected override string GetName()
	{
		return $"{this.assembly.GetName().Name} {base.GetName()}";
	}
}