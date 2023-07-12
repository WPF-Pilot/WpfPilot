namespace WpfPilot.Utility.WpfUtility.Tree;

using System.Linq;
using System.Windows;
using System.Windows.Interop;

internal class WebBrowserTreeItem : DependencyObjectTreeItem
{
	public WebBrowserTreeItem(DependencyObject target, TreeItem? parent, TreeService treeService)
		: base(target, parent, treeService)
	{
	}

	public static bool IsWebBrowserWithDevToolsSupport(DependencyObject dependencyObject)
	{
		var dependencyObjectType = dependencyObject.GetType();

		return dependencyObjectType.GetInterfaces().Any(x =>
			x.FullName is "CefSharp.IBrowserHost"
			or "CefSharp.IBrowser"
			or "CefSharp.IChromiumWebBrowserBase")
			|| IsWebView2(dependencyObject);
	}

	private static bool IsWebView2(DependencyObject dependencyObject)
	{
		if (dependencyObject is not HwndHost)
		{
			return false;
		}

		var currentType = dependencyObject.GetType();
		while (currentType is not null)
		{
			if (currentType.Name is "WebView2")
			{
				return true;
			}

			currentType = currentType.BaseType;
		}

		return false;
	}
}