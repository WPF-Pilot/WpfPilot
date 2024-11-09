namespace WpfPilot.Utility;

using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;

internal class AppRoots
{
	public AppRoots(Application app, IReadOnlyList<Visual> visuals)
	{
		App = app;
		Visuals = visuals;
	}

	// The root Application.
	public Application App { get; }

	// Other visual roots that are not the application.
	// For example, context menus or other special roots that are not within the `Application` visual tree.
	public IReadOnlyList<Visual> Visuals { get; }
}
