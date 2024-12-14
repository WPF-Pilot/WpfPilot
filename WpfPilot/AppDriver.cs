#pragma warning disable SA1027

namespace WpfPilot;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using WpfPilot.AppDriverPayload.Commands;
using WpfPilot.Interop;
using WpfPilot.Utility;
using WpfPilot.Utility.WpfUtility;
using WpfPilot.Utility.WpfUtility.Helpers;
using Node = System.Collections.Generic.Dictionary<string, object>;

public sealed class AppDriver : IDisposable
{
	private AppDriver(WpfProcess appProcess)
	{
		Temp.CleanStaleFiles();

		AppProcess = appProcess;
		ProcessCloseOnParentClose.Add(AppProcess.Process);

		// Inject the app driver into the target process.
		var pipeName = $"pid-{AppProcess.Id}-{Guid.NewGuid()}";
		var dllRootDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
		Retry.With(() =>
		{
			AppProcess.Refresh();
			Injector.InjectAppDriver(AppProcess, pipeName, dllRootDirectory);
		}, retryIntervalMs: 1000, retryCount: 30);

		// Start the communication channel.
		Channel = new NamedPipeClient(
			pipeName,
			getProcessExitCode: () =>
			{
				AppProcess.Process.Refresh();
				return AppProcess.Process.HasExited ? AppProcess.Process.ExitCode : null;
			},
			reinject: () =>
			{
				AppProcess.Process.Refresh();
				Injector.InjectAppDriver(AppProcess, pipeName, dllRootDirectory);
			});
		Keyboard = new Keyboard(OnAction);
		TargetIdToElement = new Dictionary<string, List<Element>>(capacity: 1_000); // Start with a decent capacity.
		IsElementCacheValid = false;
		IsOnAccessPropertyDisabled = false;

		// This is the default set of properties we retrieve, however devs can add more properties to this set.
		PropNames = PropNameDefaults.Value;
	}

	public void Dispose()
	{
		if (!AppProcess.Process.HasExited)
			AppProcess.Process.Kill();
	}

	/// <summary>
	/// <code>
	/// ✏️ using var appDriver = AppDriver.Launch(@"C:\Path\To\MyApp.exe", @"/debugmode /logpath c:\temp");
	/// </code>
	/// </summary>
	public static AppDriver Launch(string exePath, string? args = null)
	{
		_ = exePath ?? throw new ArgumentNullException(nameof(exePath));
		exePath = Environment.ExpandEnvironmentVariables(exePath);
		exePath = Path.GetFullPath(exePath);

		var processStartInfo = new ProcessStartInfo(exePath, args ?? "");
		var process = Process.Start(processStartInfo);
		if (process == null)
			throw new InvalidOperationException($"Failed to start process from {exePath}\nUsing args: {args}");

		var wpfProcess = new WpfProcess(process);
		return new AppDriver(wpfProcess);
	}

	/// <summary>
	/// <code>
	/// var p = new ProcessStartInfo(@"C:\Path\To\MyApp.exe");
	/// p.Environment.Add("TEST_MODE", "true");
	/// p.WorkingDirectory = @"C:\TestEnvironment";
	/// using var appDriver = AppDriver.Launch(p);
	/// </code>
	/// </summary>
	public static AppDriver Launch(ProcessStartInfo processStartInfo)
	{
		_ = processStartInfo ?? throw new ArgumentNullException(nameof(processStartInfo));

		var process = Process.Start(processStartInfo);
		if (process == null)
			throw new InvalidOperationException($"Failed to start process from file name: {processStartInfo.FileName}\nUsing args: {processStartInfo.Arguments}");

		var wpfProcess = new WpfProcess(process);
		return new AppDriver(wpfProcess);
	}

	/// <summary>
	/// <code>
	/// var p = Process.Start(new ProcessStartInfo(@"C:\Path\To\MyApp.exe"));
	/// using var appDriver = AppDriver.AttachTo(p.Id);
	/// </code>
	/// </summary>
	public static AppDriver AttachTo(int processId)
	{
		var process = Process.GetProcessById(processId);
		if (process == null)
			throw new InvalidOperationException($"Failed to find process with ID of {processId}");

		var wpfProcess = new WpfProcess(process);
		return new AppDriver(wpfProcess);
	}

	/// <summary>
	/// <code>
	/// ✏️ using var appDriver = AppDriver.AttachTo("MyCoolAppName");
	/// </code>
	/// </summary>
	public static AppDriver AttachTo(string processName)
	{
		_ = processName ?? throw new ArgumentNullException(nameof(processName));

		var process = Process.GetProcessesByName(processName).FirstOrDefault();

		// Fallback if an exact match was not found.
		if (process == null)
		{
			var processNameLower = Path.GetFileNameWithoutExtension(processName).ToLowerInvariant();
			process = Process.GetProcesses().FirstOrDefault(x =>
				{
					var pName = x.ProcessName.ToLowerInvariant();
					var wName = x.MainWindowTitle.ToLowerInvariant();
					return pName.Contains(processNameLower) || wName.Contains(processNameLower);
				});
		}

		// Couldn't get the process, give up.
		if (process == null)
		{
			var processNames = Process.GetProcesses().Select(x => x.ProcessName).Distinct().ToList();
			processNames.Sort((x, y) =>
			{
				var xScore = LevenshteinDistance.Calculate(x, processName);
				var yScore = LevenshteinDistance.Calculate(y, processName);
				return xScore < yScore ? -1 : 1;
			});

			throw new InvalidOperationException(
				$"Failed to find process with name of {processName}.\n" +
				$"Did you mean one of the following processes?\n" +
				$"{string.Join("\n", processNames.Take(5))}");
		}

		var wpfProcess = new WpfProcess(process);
		return new AppDriver(wpfProcess);
	}

	/// <summary>
	/// Finds a WPF element matching the given criteria. No order is guaranteed.
	/// <code>
	/// ✏️ appDriver.GetElement(x => x["Name"] == "My_Cool_Button");
	/// ✏️ appDriver.GetElement(x => x["Text"] == "Enter A Password");
	/// ✏️ appDriver.GetElement(x => x.TypeName == nameof(App));
	/// ✏️ appDriver.GetElement(x => x.TypeName == nameof(MainWindow));
	/// </code>
	/// </summary>
	/// <exception cref="System.TimeoutException">Thrown when no element matching the criteria is found within timeoutMs.</exception>
	public Element GetElement(Func<Element, bool?> matcher, int timeoutMs = 30_000)
	{
		_ = matcher ?? throw new ArgumentNullException(nameof(matcher));

		var start = Environment.TickCount;

		// Check cache first. This can mitigate slowness in situations where the dev has put a matcher in a tight loop.
		// There should be no cache staleness as the cache is invalidated whenever an action is taken, and we default to refreshing below.
		if (IsElementCacheValid)
		{
			foreach (var element in TargetIdToElement.Values.ToArray())
			{
				if (matcher(element[0]) == true)
					return element[0];
			}
		}

		// Simple backoff to avoid hammering the UI thread.
		var tryCount = 0;
		var delayMsByTryCount = new int[] { 25, 100, 500, 1000, 2000 };
		while (Environment.TickCount - start < timeoutMs)
		{
			var element = DoGetElement(matcher, timeoutMs);
			if (element is not null)
				return element;

			Task.Delay(tryCount < delayMsByTryCount.Length ? delayMsByTryCount[tryCount] : 1000).GetAwaiter().GetResult();
			tryCount++;
		}

		throw new TimeoutException($"Failed to find a WPF element matching criteria after {timeoutMs / 1000} seconds.");
	}

	/// <summary>
	/// Finds a WPF element matching the given criteria. No order is guaranteed.
	/// `T` must be a subclass of `Element` and have a constructor that takes a single `Element` argument.
	/// <code>
	/// ✏️ appDriver.GetElement&lt;MyCustomButton&gt;(x => x["Text"] == "Enter A Password");
	/// </code>
	/// </summary>
	/// <exception cref="System.TimeoutException">Thrown when no element matching the criteria is found within timeoutMs.</exception>
	public T GetElement<T>(Func<Element, bool?> matcher, int timeoutMs = 30_000)
		where T : Element
	{
		_ = matcher ?? throw new ArgumentNullException(nameof(matcher));

		var start = Environment.TickCount;

		// Check cache first. This can mitigate slowness in situations where the dev has put a matcher in a tight loop.
		// There should be no cache staleness as the cache is invalidated whenever an action is taken, and we default to refreshing below.
		if (IsElementCacheValid)
		{
			foreach (var element in TargetIdToElement.Values.ToArray())
			{
				if (matcher(element[0]) == true)
					return (T) Activator.CreateInstance(typeof(T), element[0])!;
			}
		}

		// Simple backoff to avoid hammering the UI thread.
		var tryCount = 0;
		var delayMsByTryCount = new int[] { 25, 100, 500, 1000, 2000 };
		while (Environment.TickCount - start < timeoutMs)
		{
			var element = DoGetElement(matcher, timeoutMs);
			if (element is not null)
				return (T) Activator.CreateInstance(typeof(T), element)!;

			Task.Delay(tryCount < delayMsByTryCount.Length ? delayMsByTryCount[tryCount] : 1000).GetAwaiter().GetResult();
			tryCount++;
		}

		throw new TimeoutException($"Failed to find a WPF element matching criteria after {timeoutMs / 1000} seconds.");
	}

	/// <summary>
	/// Finds <b>all</b> WPF elements matching the given criteria.
	/// Returns an empty list if no elements are found. No order is guaranteed.
	/// <code>
	/// ✏️ appDriver.GetElements(x => x["Name"].StartsWith("ListItem"));
	/// ✏️ appDriver.GetElements(x => x.TypeName == nameof(Button));
	/// </code>
	/// </summary>
	public IReadOnlyList<Element> GetElements(Func<Element, bool?> matcher, int timeoutMs = 30_000)
	{
		_ = matcher ?? throw new ArgumentNullException(nameof(matcher));

		var start = Environment.TickCount;

		// Check cache first. This can mitigate slowness in situations where the dev has put a matcher in a tight loop.
		// There should be no cache staleness as the cache is invalidated whenever an action is taken, and we default to refreshing below.
		if (IsElementCacheValid)
		{
			var elements = new List<Element>();
			foreach (var element in TargetIdToElement.Values.ToArray())
			{
				if (matcher(element[0]) == true)
					elements.Add(element[0]);
			}

			if (elements.Count != 0)
				return elements;
		}

		// Simple backoff to avoid hammering the UI thread.
		var tryCount = 0;
		var delayMsByTryCount = new int[] { 25, 100, 500, 1000, 2000 };
		while (Environment.TickCount - start < timeoutMs)
		{
			var elements = DoGetElements(matcher, timeoutMs);
			if (elements.Count != 0)
				return elements;

			Task.Delay(tryCount < delayMsByTryCount.Length ? delayMsByTryCount[tryCount] : 1000).GetAwaiter().GetResult();
			tryCount++;
		}

		return new Element[0];
	}

	/// <summary>
	/// Finds <b>all</b> WPF elements matching the given criteria.
	/// Returns an empty list if no elements are found. No order is guaranteed.
	/// `T` must be a subclass of `Element` and have a constructor that takes a single `Element` argument.
	/// <code>
	/// ✏️ appDriver.GetElements&lt;MyCustomListItem&gt;(x => x["Name"].StartsWith("ListItem"));
	/// </code>
	/// </summary>
	public IReadOnlyList<T> GetElements<T>(Func<Element, bool?> matcher, int timeoutMs = 30_000)
		where T : Element
	{
		_ = matcher ?? throw new ArgumentNullException(nameof(matcher));

		var start = Environment.TickCount;

		// Check cache first. This can mitigate slowness in situations where the dev has put a matcher in a tight loop.
		// There should be no cache staleness as the cache is invalidated whenever an action is taken, and we default to refreshing below.
		if (IsElementCacheValid)
		{
			var elements = new List<T>();
			foreach (var element in TargetIdToElement.Values.ToArray())
			{
				if (matcher(element[0]) == true)
					elements.Add((T) Activator.CreateInstance(typeof(T), element[0])!);
			}

			if (elements.Count != 0)
				return elements;
		}

		// Simple backoff to avoid hammering the UI thread.
		var tryCount = 0;
		var delayMsByTryCount = new int[] { 25, 100, 500, 1000, 2000 };
		while (Environment.TickCount - start < timeoutMs)
		{
			var elements = DoGetElements(matcher, timeoutMs);
			if (elements.Count != 0)
				return elements.Select(x => (T) Activator.CreateInstance(typeof(T), x)!).ToList();

			Task.Delay(tryCount < delayMsByTryCount.Length ? delayMsByTryCount[tryCount] : 1000).GetAwaiter().GetResult();
			tryCount++;
		}

		return new T[0];
	}

	/// <summary>
	/// Gets a screenshot of the app and saves it to the given path. The `AppDriver` will briefly attempt to wait for the app to be idle before taking the screenshot.
	/// <code>
	/// ✏️ appDriver.Screenshot(@"C:\pics\app-snap.png");
	/// </code>
	/// </summary>
	public void Screenshot(string fileOutputPath)
	{
		_ = fileOutputPath ?? throw new ArgumentNullException(nameof(fileOutputPath));
		fileOutputPath = Environment.ExpandEnvironmentVariables(fileOutputPath);
		fileOutputPath = Path.GetFullPath(fileOutputPath);

		var start = Environment.TickCount;
		dynamic? previousResponse = null;
		while (Environment.TickCount - start < 5000)
		{
			var response = Channel.GetResponse(new
			{
				Kind = nameof(ScreenshotCommand),
				Format = Path.GetExtension(fileOutputPath).Replace(".", ""),
			});

			var responseValue = PropInfo.GetPropertyValue(response, "Value");
			if (responseValue is string s)
			{
				if (s == "PendingResult")
					throw new TimeoutException($"{nameof(Screenshot)} timeout.");
			}

			if (previousResponse != null && previousResponse!.Base64Screenshot == response!.Base64Screenshot)
			{
				SaveImage(Convert.FromBase64String(response!.Base64Screenshot));
				return;
			}

			previousResponse = response;
			Task.Delay(500).GetAwaiter().GetResult();
		}

		SaveImage(Convert.FromBase64String(previousResponse!.Base64Screenshot));

		void SaveImage(byte[] bytes)
		{
			var folders = Path.GetDirectoryName(fileOutputPath);
			Directory.CreateDirectory(folders);
			File.WriteAllBytes(fileOutputPath, bytes);
		}
	}

	/// <summary>
	/// Gets a screenshot of the app. Returns the bytes of the image. The `AppDriver` will briefly attempt to wait for the app to be idle before taking the screenshot.
	/// <code>
	/// var bytes = appDriver.Screenshot();
	/// File.WriteAllBytes(@"C:\test-pic.jpg", bytes);
	/// </code>
	/// </summary>
	public byte[] Screenshot(ImageFormat format = ImageFormat.Jpeg)
	{
		var start = Environment.TickCount;
		dynamic? previousResponse = null;
		while (Environment.TickCount - start < 5000)
		{
			var response = Channel.GetResponse(new
			{
				Kind = nameof(ScreenshotCommand),
				Format = format.ToString().ToLowerInvariant(),
			});

			var responseValue = PropInfo.GetPropertyValue(response, "Value");
			if (responseValue is string s)
			{
				if (s == "PendingResult")
					throw new TimeoutException($"{nameof(Screenshot)} timeout.");
			}

			if (previousResponse != null && previousResponse!.Base64Screenshot == response!.Base64Screenshot)
				return Convert.FromBase64String(response!.Base64Screenshot);

			previousResponse = response;
			Task.Delay(500).GetAwaiter().GetResult();
		}

		var bytes = Convert.FromBase64String(previousResponse!.Base64Screenshot);
		return bytes;
	}

	/// <summary>
	/// Records the window with the given title, or the fullscreen if `null` is passed. Returns an IDisposable that stops recording when disposed.
	/// The recording must be stopped (disposed), or the recording will be corrupt. Only one recording at a time is allowed; multiple calls will end the original recording.
	/// <code>
	/// ✏️ using var recordWindow = AppDriver.Record(@"C:\videos\test-vid.mp4", appDriver.Process.MainWindowTitle);
	/// ✏️ using var recordFullscreen = AppDriver.Record(@"C:\videos\test-vid.mp4");
	/// </code>
	/// </summary>
	public static IDisposable Record(string fileOutputPath, string? windowTitle = null)
	{
		_ = fileOutputPath ?? throw new ArgumentNullException(nameof(fileOutputPath));

		// Stop any existing recording.
		if (RecordingTask is not null)
			RecordingTask.Dispose();

		fileOutputPath = Environment.ExpandEnvironmentVariables(fileOutputPath);
		fileOutputPath = Path.GetFullPath(fileOutputPath);

		// Ensure directories exist.
		var folders = Path.GetDirectoryName(fileOutputPath);
		Directory.CreateDirectory(folders);

		// Delete the file if it already exists (in case FFmpeg `-y` arg doesn't work).
		if (File.Exists(fileOutputPath))
			File.Delete(fileOutputPath);

		// Record just the given window if a window title is passed.
		var fullScreen = windowTitle == null;

		// Fallback to fullscreen if there are multiple windows with the same title.
		if (fullScreen)
		{
			var hasMultipleWindowTitleMatches = Process.GetProcesses().Count(x => x.MainWindowTitle == windowTitle) > 1;
			if (hasMultipleWindowTitleMatches)
				fullScreen = true;
		}

		// Start FFmpeg.
		var ffmpeg = new Process
		{
			EnableRaisingEvents = true,
			StartInfo = new ProcessStartInfo
			{
				FileName = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), @"WpfPilotResources\ffmpeg.exe"),
				Arguments = fullScreen ?
					$"-y -f gdigrab -framerate 24 -i desktop \"{fileOutputPath}\" -c:v vp8" :
					$"-y -f gdigrab -framerate 24 -i title=\"{windowTitle}\" \"{fileOutputPath}\" -c:v vp8",
				CreateNoWindow = true,
				UseShellExecute = false,
				RedirectStandardError = true,
				RedirectStandardOutput = true,
				RedirectStandardInput = true,
			}
		};

		// Log FFmpeg output.
		ffmpeg.OutputDataReceived += (s, e) =>
		{
			if (e.Data != null)
				Log.Info($"[FFmpeg] {e.Data}");
		};
		ffmpeg.ErrorDataReceived += (s, e) =>
		{
			if (e.Data != null)
				Log.Info($"[FFmpeg] {e.Data}");
		};

		ffmpeg.Start();
		ProcessCloseOnParentClose.Add(ffmpeg);

		// Wait for FFmpeg to start.
		ffmpeg.BeginOutputReadLine();
		ffmpeg.BeginErrorReadLine();

		// Stop recording on Dispose.
		RecordingTask = new ScopeGuard(
			exitAction: () =>
			{
				// Stop FFmpeg.
				StreamWriter inputWriter = ffmpeg.StandardInput;
				inputWriter.WriteLine("q");

				ffmpeg.WaitForExit();
				ffmpeg.Close();
				inputWriter.Close();

				RecordingTask = null;
			});

		return RecordingTask;
	}

	private Element? DoGetElement(Func<Element, bool?> matcher, int timeoutMs)
	{
		var response = Channel.GetResponse(new
		{
			Kind = nameof(GetVisualTreeCommand),
			PropNames,
			TimeoutMs = timeoutMs,
		}, timeoutMs)!;

		var responseValue = PropInfo.GetPropertyValue(response, "Value");
		if (responseValue is string s)
		{
			if (s == "PendingResult")
				throw new TimeoutException($"{nameof(GetElement)} timeout.");
		}

		List<Node> nodes = response;

		// Refresh any tracked elements and return the matched element.
		RefreshVisualTree(nodes);
		IsElementCacheValid = true;

		Element? match = null;
		foreach (var element in TargetIdToElement.Values.ToArray())
		{
			if (matcher(element[0]) == true)
			{
				if (match == null)
					match = element[0];

				// Update matchers.
				foreach (var e in element)
					e.Matcher = matcher;
			}
		}

		return match;
	}

	private IReadOnlyList<Element> DoGetElements(Func<Element, bool?> matcher, int timeoutMs)
	{
		var response = Channel.GetResponse(new
		{
			Kind = nameof(GetVisualTreeCommand),
			PropNames,
			TimeoutMs = timeoutMs,
		}, timeoutMs)!;

		var responseValue = PropInfo.GetPropertyValue(response, "Value");
		if (responseValue is string s)
		{
			if (s == "PendingResult")
				throw new TimeoutException($"{nameof(GetElements)} timeout.");
		}

		List<Node> nodes = response;

		// Refresh any tracked elements and return the matched elements.
		RefreshVisualTree(nodes);
		IsElementCacheValid = true;

		var matches = new List<Element>();
		foreach (var element in TargetIdToElement.Values.ToArray())
		{
			if (matcher(element[0]) == true)
			{
				matches.Add(element[0]);

				// Update matchers.
				foreach (var e in element)
					e.Matcher = matcher;
			}
		}

		return matches;
	}

	private void OnAccessProperty(string propName)
	{
		if (IsOnAccessPropertyDisabled)
			return;

		if (IsElementCacheValid && PropNames.Contains(propName))
			return;

		// Add the property to the set of properties we're tracking and refresh the visual tree.
		if (!PropNames.Contains(propName))
			PropNames.Add(propName);

		List<Node> nodes = Channel.GetResponse(new
		{
			Kind = nameof(GetVisualTreeCommand),
			PropNames,
		})!;

		RefreshVisualTree(nodes);
		IsElementCacheValid = true;
	}

	private void OnAction()
	{
		IsElementCacheValid = false;
	}

	private void RefreshVisualTree(List<Node> nodes)
	{
		var staleTargetIds = new HashSet<string>(TargetIdToElement.Keys);

		foreach (var node in nodes)
		{
			dynamic unwrappedProperties = ((Dictionary<string, object>) node["Properties"])
				.ToDictionary(
					x => x.Key,
					x => PropInfo.GetPropertyValue(x.Value, "Value")); // Expects `x.Value` to be a `WrappedArg<T>`.
			node.TryGetValue("ParentId", out var parentIdObject);
			var targetId = (string) node["TargetId"];
			var typeName = (string) node["TypeName"];
			var childIds = (string[]) node["ChildIds"];
			var parentId = (string?) parentIdObject;

			// Remove the target ID from the stale set.
			staleTargetIds.Remove(targetId);

			// Refresh old elements users may have references to.
			if (TargetIdToElement.ContainsKey(targetId))
			{
				foreach (var element in TargetIdToElement[targetId])
					element.Refresh(typeName, unwrappedProperties, parentId, childIds);
			}
			else
			{
				TargetIdToElement[targetId] = new List<Element>()
				{
					new Element(
						targetId,
						typeName,
						parentId,
						childIds,
						unwrappedProperties,
						TargetIdToElement,
						Channel,
						(Action) OnAction,
						(Action<string>) OnAccessProperty)
				};
			}
		}

		// Temp disable OnAccessProperty to avoid infinite loops when using `staleElement.Matcher`.
		IsOnAccessPropertyDisabled = true;

		// Fix stale elements, if possible.
		foreach (var staleTargetId in staleTargetIds)
		{
			var staleElement = TargetIdToElement[staleTargetId][0];

			staleElement.Properties.TryGetValue("Name", out var staleName);
			staleElement.Properties.TryGetValue("Title", out var staleTitle);
			staleElement.Properties.TryGetValue("AutomationProperties.Name", out var staleAutomationName);
			staleElement.Properties.TryGetValue("AutomationProperties.AutomationId", out var staleAutomationId);
			var hasWidth = staleElement.Properties.TryGetValue("ActualWidth", out var staleActualWidth);
			var hasHeight = staleElement.Properties.TryGetValue("ActualHeight", out var staleActualHeight);

			// Nothing to match with.
			if (IsEmpty(staleName) && IsEmpty(staleTitle) && IsEmpty(staleAutomationName) && IsEmpty(staleAutomationId) && staleElement.Matcher == null)
				continue;

			var candidates = new HashSet<(Element Element, int Score)>();

			foreach (var element in TargetIdToElement.Values.SelectMany(x => x))
			{
				// Ignore stale elements.
				if (staleTargetIds.Contains(element.TargetId))
					continue;

				// Check for match.
				if (!IsEmpty(staleAutomationId) && element.Properties.TryGetValue("AutomationProperties.AutomationId", out var automationId) && staleAutomationId == automationId)
				{
					candidates.Add((element, 100));
					continue;
				}

				// Check for match.
				if (!IsEmpty(staleAutomationName) && element.Properties.TryGetValue("AutomationProperties.Name", out var automationName) && staleAutomationName == automationName)
				{
					candidates.Add((element, 100));
					continue;
				}

				// Check for match.
				if (!IsEmpty(staleName) && element.Properties.TryGetValue("Name", out var name) && staleName == name)
				{
					candidates.Add((element, 50));
					continue;
				}

				// Check for match.
				if (!IsEmpty(staleTitle) && element.Properties.TryGetValue("Title", out var title) && staleTitle == title)
				{
					candidates.Add((element, 5));
					continue;
				}

				// Check for match.
				if (staleElement.Matcher != null && staleElement.Matcher(element) == true)
				{
					candidates.Add((element, 1));
					continue;
				}
			}

			var sorted = candidates.OrderBy(x =>
			{
				var score = x.Score;

				// Prefer elements with matching width and height.
				if (hasWidth && x.Element.Properties.TryGetValue("ActualWidth", out var actualWidth) && staleActualWidth == actualWidth)
					score += 50;
				if (hasHeight && x.Element.Properties.TryGetValue("ActualHeight", out var actualHeight) && staleActualHeight == actualHeight)
					score += 50;

				return score;
			}).ToList();

			if (sorted.Count != 0)
			{
				var bestMatch = sorted.Last().Element;
				foreach (var e in TargetIdToElement[staleTargetId])
				{
					e.TargetId = bestMatch.TargetId;
					e.Refresh(bestMatch.TypeName, bestMatch.Properties, bestMatch.ParentId, bestMatch.ChildIds);
					TargetIdToElement[e.TargetId].Add(e);
				}
			}
		}

		IsOnAccessPropertyDisabled = false;

		// Remove stale elements.
		foreach (var staleTargetId in staleTargetIds)
			TargetIdToElement.Remove(staleTargetId);
	}

	private static bool IsEmpty(object? s)
	{
		return s == null || (s is string str && str == "");
	}

	public Keyboard Keyboard { get; }
	public Process Process => AppProcess.Process;

	private WpfProcess AppProcess { get; }
	private NamedPipeClient Channel { get; }
	private HashSet<string> PropNames { get; }
	private Dictionary<string, List<Element>> TargetIdToElement { get; }
	private bool IsElementCacheValid { get; set; }
	private bool IsOnAccessPropertyDisabled { get; set; }
	private static IDisposable? RecordingTask { get; set; }
}
