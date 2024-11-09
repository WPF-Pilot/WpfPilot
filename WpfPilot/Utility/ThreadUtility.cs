namespace WpfPilot.Utility;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

internal static class ThreadUtility
{
	public static UIThreadRunResult RunOnUIThread(Func<object, Task> action)
	{
		var presentationSources = new List<PresentationSource>();
		foreach (PresentationSource? presentationSource in PresentationSource.CurrentSources)
		{
			if (presentationSource == null)
				continue;
			presentationSources.Add(presentationSource);
		}

		foreach (var presentationSource in presentationSources)
		{
			var rootVisual = presentationSource.RunInDispatcher(() => presentationSource.RootVisual);

			object rootObject = rootVisual;
			if (Application.Current is not null && Application.Current.Dispatcher == presentationSource.Dispatcher)
				rootObject = Application.Current;

			if (rootObject is null)
				continue;

			var dispatcher = (rootObject as DispatcherObject)?.Dispatcher ?? presentationSource.Dispatcher;
			if (dispatcher != null && rootObject is Application app)
			{
				var roots = presentationSources.Where(p => p != presentationSource).Select(x => x.RootVisual).ToList();
				var appRoot = new AppRoots(app, roots);
				var result = dispatcher.InvokeAsync(async () => { await action(appRoot); });

				// WARN: This does NOT guarantee that the async action has completed, merely that it has run up to the first await.
				// Once the first await is hit, the dispatcher will return to the main loop and continue processing messages, and the remainder of the async
				// work will run later.
				result.GetAwaiter().GetResult();

				return UIThreadRunResult.Finished;
			}
		}

		return UIThreadRunResult.Unable;
	}

	public static Task<UIThreadRunResult> RunOnStaThread(Func<Task<UIThreadRunResult>> func)
	{
		var tcs = new TaskCompletionSource<UIThreadRunResult>();

		Thread thread = new Thread(() =>
		{
			try
			{
				var result = func().GetAwaiter().GetResult();
				tcs.SetResult(result);
			}
			catch (Exception ex)
			{
				tcs.SetException(ex);
			}
		});

		thread.SetApartmentState(ApartmentState.STA);
		thread.IsBackground = true;
		thread.Start();

		return tcs.Task;
	}

	private static T RunInDispatcher<T>(this DispatcherObject? dispatcherObject, Func<T> action, DispatcherPriority priority = DispatcherPriority.Normal)
	{
		if (dispatcherObject is null)
			return action();

		var dispatcher = dispatcherObject.Dispatcher;
		if (dispatcher.CheckAccess())
			return action();

		return (T) dispatcher.Invoke(priority, action);
	}

	internal enum UIThreadRunResult
	{
		Unable,
		Finished,
		Pending, // Could occur when `ShowDialog` is invoked.
	}
}
