#pragma warning disable SA1401

namespace WpfPilot.Utility.WpfUtility.Tree;

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using WpfPilot.Interop;
using WpfPilot.Utility.WpfUtility;
using WpfPilot.Utility.WpfUtility.Diagnostics;
using WpfPilot.Utility.WpfUtility.Helpers;

internal sealed class TreeService : IDisposable, INotifyPropertyChanged
{
    // ... (existing code)

    public IEnumerable GetChildren(TreeItem treeItem)
    {
        if (treeItem.OmitChildren)
        {
            return Enumerable.Empty<object>();
        }

        return this.GetChildren(treeItem.Target);
    }

    public IEnumerable GetChildren(object target)
    {
        if (target is not DependencyObject dependencyObject || (target is Visual == false && target is Visual3D == false))
        {
            yield break;
        }

        var childrenCount = VisualTreeHelper.GetChildrenCount(dependencyObject);

        for (var i = 0; i < childrenCount; i++)
        {
            var child = VisualTreeHelper.GetChild(dependencyObject, i);
            yield return child;
        }

        // Add support for ContextMenu items
        if (dependencyObject is FrameworkElement frameworkElement && frameworkElement.ContextMenu != null)
        {
            foreach (var item in frameworkElement.ContextMenu.Items)
            {
                yield return item;
            }
        }
    }

    public TreeItem Construct(object target, TreeItem? parent, bool omitChildren = false)
    {
        var treeItem = target switch
        {
            // ... (existing cases)
            MenuItem typedTarget => new MenuItemTreeItem(typedTarget, parent, this),
            ContextMenu typedTarget => new ContextMenuTreeItem(typedTarget, parent, this),
            _ => new TreeItem(target, parent, this)
        };

        // ... (rest of the existing code)
    }

    // ... (rest of the existing methods)
}

internal class MenuItemTreeItem : TreeItem
{
    public MenuItemTreeItem(MenuItem target, TreeItem? parent, TreeService treeService)
        : base(target, parent, treeService)
    {
    }

    public override IEnumerable GetChildren()
    {
        var menuItem = (MenuItem)Target;
        return menuItem.Items.Cast<object>();
    }
}

internal class ContextMenuTreeItem : TreeItem
{
    public ContextMenuTreeItem(ContextMenu target, TreeItem? parent, TreeService treeService)
        : base(target, parent, treeService)
    {
    }

    public override IEnumerable GetChildren()
    {
        var contextMenu = (ContextMenu)Target;
        return contextMenu.Items.Cast<object>();
    }
}
{
	public List<Dictionary<string, object?>> AllNodes;
	private TreeItem? rootTreeItem;
	private HashSet<string> propNames;
	private HashSet<Guid> visitedTargets;

	public TreeService(HashSet<string> propNames)
	{
		this.DiagnosticContext = new DiagnosticContext(this);
		this.propNames = propNames;
		this.visitedTargets = new HashSet<Guid>();
		this.AllNodes = new List<Dictionary<string, object?>>(propNames.Count > 0 ? 1_000 : 0);
	}

	public TreeItem? RootTreeItem
	{
		get => this.rootTreeItem;
		set
		{
			if (Equals(value, this.rootTreeItem))
			{
				return;
			}

			this.rootTreeItem = value;

			this.OnPropertyChanged();
		}
	}

	public DiagnosticContext DiagnosticContext { get; }

	public IEnumerable GetChildren(TreeItem treeItem)
	{
		if (treeItem.OmitChildren)
		{
			return Enumerable.Empty<object>();
		}

		return this.GetChildren(treeItem.Target);
	}

	public IEnumerable GetChildren(object target)
	{
		if (target is not DependencyObject dependencyObject || (target is Visual == false && target is Visual3D == false))
		{
			yield break;
		}

		var childrenCount = VisualTreeHelper.GetChildrenCount(dependencyObject);

		for (var i = 0; i < childrenCount; i++)
		{
			var child = VisualTreeHelper.GetChild(dependencyObject, i);
			yield return child;
		}
	}

	public TreeItem Construct(object target, TreeItem? parent, bool omitChildren = false)
	{
		var treeItem = target switch
		{
			AutomationPeer typedTarget => new AutomationPeerTreeItem(typedTarget, parent, this),
			ResourceDictionaryWrapper typedTarget => new ResourceDictionaryTreeItem(typedTarget, parent, this),
			ResourceDictionary typedTarget => new ResourceDictionaryTreeItem(typedTarget, parent, this),
			System.Windows.Application typedTarget => new ApplicationTreeItem(typedTarget, parent, this),
			Window typedTarget => new WindowTreeItem(typedTarget, parent, this),
			Popup typedTarget => new PopupTreeItem(typedTarget, parent, this),
			System.Windows.Controls.Image typedTarget => new ImageTreeItem(typedTarget, parent, this),
			DependencyObject typedTarget => this.ConstructFromDependencyObject(parent, typedTarget),
			_ => new TreeItem(target, parent, this)
		};

		treeItem.OmitChildren = omitChildren;
		treeItem.Reload();

		if (this.propNames.Count != 0 && !visitedTargets.Contains(treeItem.Target.GetRefId()))
		{
			visitedTargets.Add(treeItem.Target.GetRefId());

			var propertyDict = new Dictionary<string, object>();
			var properties = PropInfo.GetProperties(treeItem.Target, propNames);

			// Maintainers note: When changing any of these values, consider adding a converter for the string representation in `RemoteCommands.SetPropertyCommand`.
			// The above allows devs to more intuitively set these values in the test suite. Eg `element["FontWeight"] = "Light";`
			foreach (var property in properties)
			{
				var value = property.Value switch
				{
					// Newtonsoft can serialize these types fine.
					// The issue is the client (test suite) may not have access to the types, so we serialize them as strings.
					Type theType => theType.FullName,
					Enum theEnum => theEnum.ToString(),
					Brush theBrush => theBrush.ToString(),
					FontFamily theFontFamily => theFontFamily.ToString(),

					// These types can be serialized by Newtsonsoft and the client, but people would rather have a string representation.
					Size theSize => theSize.ToString(),
					Point thePoint => thePoint.ToString(),
					Thickness theThickness => theThickness.ToString(),
					Rect theRect => theRect.ToString(),
					FontWeight theFontWeight => theFontWeight.ToString(),
					FontStyle theFontStyle => theFontStyle.ToString(),
					FontStretch theFontStretch => theFontStretch.ToString(),

					// Just pass the value through.
					Guid => property.Value,
					DateTime => property.Value,
					string => property.Value,
					_ when property.Value != null && property.Value.GetType().IsPrimitive => property.Value,

					// Otherwise default to `ToString()`. The underlying type cannot be safely used, because the client may not have access to the type.
					_ => property.Value?.ToString(),
				};
				if (value is not null)
					propertyDict[property.Name] = WrappedArg<object>.Wrap(value)!;
			}

			var node = new Dictionary<string, object?>
			{
				["TargetId"] = treeItem.Target.GetRefId(),
				["TypeName"] = treeItem.Target.GetType().Name,
				["Properties"] = propertyDict,
				["ParentId"] = parent?.Target?.GetRefId(),
				["ChildIds"] = treeItem.Children.Select(x => x.Target.GetRefId().ToString()).ToArray(),
			};
			AllNodes.Add(node);
		}

		if (parent is null)
		{
			// If the parent is null this should be our new root element
			this.RootTreeItem = treeItem;

			foreach (var child in treeItem.Children)
			{
				if (child is ResourceDictionaryTreeItem)
				{
					continue;
				}

				child.ExpandTo();
			}

			return this.RootTreeItem;
		}

		return treeItem;
	}

	private DependencyObjectTreeItem ConstructFromDependencyObject(TreeItem? parent, DependencyObject dependencyObject)
	{
		if (WebBrowserTreeItem.IsWebBrowserWithDevToolsSupport(dependencyObject))
		{
			return new WebBrowserTreeItem(dependencyObject, parent, this);
		}

		return new DependencyObjectTreeItem(dependencyObject, parent, this);
	}

	public void Dispose()
	{
		this.DiagnosticContext.Dispose();
	}

	public event PropertyChangedEventHandler? PropertyChanged;

	public void OnPropertyChanged([CallerMemberName] string? propertyName = null)
	{
		this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
	}
}