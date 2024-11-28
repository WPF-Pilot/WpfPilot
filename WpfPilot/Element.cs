namespace WpfPilot;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Newtonsoft.Json;
using WpfPilot.AppDriverPayload;
using WpfPilot.AppDriverPayload.Commands;
using WpfPilot.Assert;
using WpfPilot.Interop;
using WpfPilot.Utility.WpfUtility;

public class Element
{
	// Public constructor to support inheritance.
	public Element(Element other)
	{
		_ = other ?? throw new ArgumentNullException(nameof(other));

		TargetId = other.TargetId;
		TypeName = other.TypeName;
		ChildIds = other.ChildIds;
		Properties = other.Properties;
		TargetIdToElement = other.TargetIdToElement;
		Channel = other.Channel;
		OnAction = other.OnAction;
		OnAccessProperty = other.OnAccessProperty;
		ParentId = other.ParentId;

		// Register the element so it receives updates.
		TargetIdToElement[TargetId].Add(this);
	}

	// Internal constructor managed by `AppDriver`.
	internal Element(
		string targetId,
		string typeName,
		string? parentId,
		IReadOnlyList<string> childIds,
		IReadOnlyDictionary<string, object> properties,
		IReadOnlyDictionary<string, List<Element>> targetIdToElement,
		NamedPipeClient channel,
		Action onAction,
		Action<string> onAccessProperty)
	{
		TargetId = targetId ?? throw new ArgumentNullException(nameof(targetId));
		TypeName = typeName ?? throw new ArgumentNullException(nameof(typeName));
		Properties = properties ?? throw new ArgumentNullException(nameof(properties));
		ChildIds = childIds ?? throw new ArgumentNullException(nameof(childIds));
		TargetIdToElement = targetIdToElement ?? throw new ArgumentNullException(nameof(targetIdToElement));
		Channel = channel ?? throw new ArgumentNullException(nameof(channel));
		OnAction = onAction ?? throw new ArgumentNullException(nameof(onAction));
		OnAccessProperty = onAccessProperty ?? throw new ArgumentNullException(nameof(onAccessProperty));
		ParentId = parentId;
	}

	public virtual Element Click() =>
		DoClick("Left");

	public virtual Element RightClick() =>
		DoClick("Right");

	public virtual Element DoubleClick()
	{
		return RaiseEvent<UIElement>(_ => new MouseButtonEventArgs(InputManager.Current.PrimaryMouseDevice, Environment.TickCount, MouseButton.Left)
		{
			RoutedEvent = Control.MouseDoubleClickEvent,
		});
	}

	public virtual Element Focus()
	{
		return Invoke(element => element.Focus());
	}

	public virtual Element Select()
	{
		return SetProperty("IsSelected", true);
	}

	public virtual Element Expand()
	{
		return SetProperty("IsExpanded", true);
	}

	public virtual Element Collapse()
	{
		return SetProperty("IsExpanded", false);
	}

	public virtual Element SelectText(string text)
	{
		var currentText = (string) Properties["Text"];
		var startIndex = currentText.IndexOf(text, StringComparison.Ordinal);

		// If the text does not exist in the current text, then just set the text.
		if (startIndex == -1)
		{
			SetProperty("SelectedText", text);
			return this;
		}

		SetProperty("SelectionStart", startIndex);
		SetProperty("SelectionLength", text.Length);
		return this;
	}

	public virtual Element Check()
	{
		return SetProperty("IsChecked", true);
	}

	public virtual Element Uncheck()
	{
		return SetProperty("IsChecked", false);
	}

	public virtual Element ScrollIntoView()
	{
		return Invoke<FrameworkElement>(element => element.BringIntoView());
	}

	public virtual Element Type(string text)
	{
		// Many controls don't require focus to be set before typing, but some do.
		Invoke(element => element.Focus());

		Expression<Action<Application>> code = app => KeyboardInput.Type(text);
		var response = Channel.GetResponse(new
		{
			Kind = nameof(InvokeStaticCommand),
			Code = Eval.SerializeCode(code)
		});

		OnAction();
		return this;
	}

	/// <summary>
	/// Gets a screenshot of the element and saves it to the given path. The code will briefly attempt to wait for the element to be idle before taking the screenshot.
	/// <code>
	/// ✏️ element.Screenshot(@"C:\screenshot.png");
	/// </code>
	/// </summary>
	public virtual Element Screenshot(string fileOutputPath)
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
				Format = Path.GetExtension(fileOutputPath).Replace(".", ""), // "png", "jpg", etc.
				TargetId,
			});

			if (previousResponse != null && previousResponse!.Base64Screenshot == response!.Base64Screenshot)
			{
				SaveImage(Convert.FromBase64String(response!.Base64Screenshot));
				return this;
			}

			previousResponse = response;
			Task.Delay(500).GetAwaiter().GetResult();
		}

		SaveImage(Convert.FromBase64String(previousResponse!.Base64Screenshot));

		void SaveImage(byte[] bytes)
		{
			// Ensure directories exist and save the image.
			var folders = Path.GetDirectoryName(fileOutputPath);
			Directory.CreateDirectory(folders);
			File.WriteAllBytes(fileOutputPath, bytes);
		}

		return this;
	}

	/// <summary>
	/// Gets a screenshot of the element. Returns the bytes of the image. The code will briefly attempt to wait for the element to be idle before taking the screenshot.
	/// <code>
	/// element.Screenshot(out var bytes);
	/// File.WriteAllBytes(@"C:\test-pic.jpg", bytes);
	/// </code>
	/// </summary>
	public virtual Element Screenshot(out byte[] screenshotBytes, ImageFormat format = ImageFormat.Jpeg)
	{
		var start = Environment.TickCount;
		dynamic? previousResponse = null;
		while (Environment.TickCount - start < 5000)
		{
			var response = Channel.GetResponse(new
			{
				Kind = nameof(ScreenshotCommand),
				Format = format.ToString().ToLowerInvariant(),
				TargetId,
			});

			if (previousResponse != null && previousResponse!.Base64Screenshot == response!.Base64Screenshot)
			{
				screenshotBytes = Convert.FromBase64String(response!.Base64Screenshot);
				return this;
			}

			previousResponse = response;
			Task.Delay(500).GetAwaiter().GetResult();
		}

		screenshotBytes = Convert.FromBase64String(previousResponse!.Base64Screenshot);
		return this;
	}

	/// <summary>
	/// Gets a screenshot of the element. Returns the bytes of the image. The code will briefly attempt to wait for the element to be idle before taking the screenshot.
	/// <code>
	/// var bytes = element.Screenshot();
	/// File.WriteAllBytes(@"C:\test-pic.jpg", bytes);
	/// </code>
	/// </summary>
	public byte[] Screenshot(ImageFormat format = ImageFormat.Jpeg)
	{
		Screenshot(out var bytes, format);
		return bytes;
	}

	/// <summary>
	/// <code>
	/// ✏️ element.RaiseEvent(() => new RoutedEventArgs(ButtonBase.ClickEvent));
	/// </code>
	/// </summary>
	public virtual Element RaiseEvent<TInput>(Expression<Func<TInput, RoutedEventArgs>> code)
		where TInput : UIElement
	{
		var response = Channel.GetResponse(new
		{
			Kind = nameof(RaiseEventCommand),
			TargetId,
			GetRoutedEventArgs = Eval.SerializeCode(code),
		});

		OnAction();
		return this;
	}

	/***************** non-async versions of `Invoke`. *****************/

	/// <summary>
	/// Executes the given expression on the underlying control and returns the result.
	/// <code>
	/// ✏️ myCustomControl.Invoke&lt;CustomControl, FormResult&gt;(x => x.ResetForm());
	/// ✏️ calendar.Invoke&lt;Calendar, DayOfWeek&gt;(x => x.FirstDayOfWeek);
	/// </code>
	/// </summary>
	/// <typeparam name="TInput">The type of the underlying control. E.G. Button, MenuItem, CheckBox, or a custom control.</typeparam>
	/// <typeparam name="TOutput">The result type of the expression call.</typeparam>
	/// <param name="code">The expression to run on the control.</param>
	/// <param name="timeoutMs">The maximum amount of time to give the expression to execute.</param>
	/// <returns>The result of the expression call.</returns>
	/// <exception cref="SerializationException">
	/// The result was not JSON serializable over the wire. In this case, consider returning a subset of the object, instead of the entire object.
	/// E.G.
	/// myCustomForm.Invoke&lt;CustomForm, FormViewModel&gt;(x => x.ViewModel) becomes
	/// myCustomForm.Invoke&lt;CustomForm, bool&gt;(x => x.ViewModel.HasSubmitted)
	/// Another option is not returning the result by calling the non-return version of Invoke.
	/// </exception>
	/// <exception cref="TimeoutException">
	/// The Invoke expression ran for too long and was cut short.
	/// </exception>
	public TOutput Invoke<TInput, TOutput>(Expression<Func<TInput, TOutput>> code, int timeoutMs = 10_000)
	{
		var response = Channel.GetResponse(new
		{
			Kind = nameof(InvokeCommand),
			TargetId,
			Code = Eval.SerializeCode(code),
			TimeoutMs = timeoutMs
		}, timeoutMs: timeoutMs);

		var responseValue = PropInfo.GetPropertyValue(response, "Value");
		if (responseValue is string s)
		{
			if (s == "UnserializableResult")
				throw new SerializationException($"{nameof(Invoke)} result is not serializable.");
			if (s == "PendingResult")
				throw new TimeoutException($"{nameof(Invoke)} timeout.");
		}

		var result = (TOutput) responseValue;

		OnAction();
		return result!;
	}

	/// <summary>
	/// Executes the given expression on the underlying control and returns the result.
	/// <code>
	/// ✏️ myCustomControl.Invoke&lt;CustomControl, FormResult&gt;(x => x.ResetForm(), out var resetFormResult);
	/// ✏️ calendar.Invoke&lt;Calendar, DayOfWeek&gt;(x => x.FirstDayOfWeek, out var firstDayOfWeek);
	/// </code>
	/// </summary>
	/// <typeparam name="TInput">The type of the underlying control. E.G. Button, MenuItem, CheckBox, or a custom control.</typeparam>
	/// <typeparam name="TOutput">The result type of the expression call.</typeparam>
	/// <param name="code">The expression to run on the control.</param>
	/// <param name="result">The output variable that holds the result of the expression call.</param>
	/// <param name="timeoutMs">The maximum amount of time to give the expression to execute.</param>
	/// <returns>The element so additional methods can be chained and the result of the expression call in the out variable.</returns>
	/// <exception cref="SerializationException">
	/// The result was not JSON serializable over the wire. In this case, consider returning a subset of the object, instead of the entire object.
	/// E.G.
	/// myCustomForm.Invoke&lt;CustomForm, FormViewModel&gt;(x => x.ViewModel) becomes
	/// myCustomForm.Invoke&lt;CustomForm, bool&gt;(x => x.ViewModel.HasSubmitted)
	/// Another option is not returning the result by calling the non-return version of Invoke.
	/// </exception>
	/// <exception cref="TimeoutException">
	/// The Invoke expression ran for too long and was cut short.
	/// </exception>
	public virtual Element Invoke<TInput, TOutput>(Expression<Func<TInput, TOutput>> code, out TOutput result, int timeoutMs = 10_000)
	{
		var response = Channel.GetResponse(new
		{
			Kind = nameof(InvokeCommand),
			TargetId,
			Code = Eval.SerializeCode(code),
			TimeoutMs = timeoutMs,
		}, timeoutMs: timeoutMs);

		var responseValue = PropInfo.GetPropertyValue(response, "Value");
		if (responseValue is string s)
		{
			if (s == "UnserializableResult")
				throw new SerializationException($"Unserializable {nameof(Invoke)} result received.");
			if (s == "PendingResult")
				throw new TimeoutException($"{nameof(Invoke)} timeout.");
		}

		result = (TOutput) responseValue;

		OnAction();
		return this;
	}

	/// <summary>
	/// Executes the given expression on the underlying control.
	/// <code>
	/// ✏️ calendar.Invoke(x => x.ReleaseAllTouchCaptures());
	/// </code>
	/// </summary>
	/// <param name="code">The expression to run on the control.</param>
	/// <param name="timeoutMs">The maximum amount of time to give the expression to execute.</param>
	/// <returns>The element so additional methods can be chained.</returns>
	/// <exception cref="TimeoutException">
	/// The Invoke expression ran for too long and was cut short.
	/// </exception>
	public virtual Element Invoke(Expression<Action<UIElement>> code, int timeoutMs = 10_000)
	{
		var response = Channel.GetResponse(new
		{
			Kind = nameof(InvokeCommand),
			TargetId,
			Code = Eval.SerializeCode(code),
			TimeoutMs = timeoutMs,
		}, timeoutMs: timeoutMs);

		var responseValue = PropInfo.GetPropertyValue(response, "Value");
		if (responseValue is string s)
		{
			if (s == "PendingResult")
				throw new TimeoutException($"{nameof(Invoke)} timeout.");
		}

		OnAction();
		return this;
	}

	/// <summary>
	/// Executes the given expression on the underlying control.
	/// <code>
	/// ✏️ calendar.Invoke&lt;Calendar&gt;(x => x.ResetCalendar());
	/// </code>
	/// </summary>
	/// <param name="code">The expression to run on the control.</param>
	/// <param name="timeoutMs">The maximum amount of time to give the expression to execute.</param>
	/// <returns>The element so additional methods can be chained.</returns>
	/// <exception cref="TimeoutException">
	/// The Invoke expression ran for too long and was cut short.
	/// </exception>
	public virtual Element Invoke<TInput>(Expression<Action<TInput>> code, int timeoutMs = 10_000)
	{
		var response = Channel.GetResponse(new
		{
			Kind = nameof(InvokeCommand),
			TargetId,
			Code = Eval.SerializeCode(code),
			TimeoutMs = timeoutMs,
		}, timeoutMs: timeoutMs);

		var responseValue = PropInfo.GetPropertyValue(response, "Value");
		if (responseValue is string s)
		{
			if (s == "PendingResult")
				throw new TimeoutException($"{nameof(Invoke)} timeout.");
		}

		OnAction();
		return this;
	}

	/***************** async versions of `Invoke`. *****************/

	/// <summary>
	/// Executes the given async expression on the underlying control and returns the result.<br/>
	/// `await` should not be used in the expression, this is handled by WPF Pilot.
	/// <code>
	/// ✏️ myCustomControl.InvokeAsync&lt;CustomControl, FormResult&gt;(x => x.ResetFormAsync());
	/// ✏️ calendar.InvokeAsync&lt;Calendar, DayOfWeek&gt;(x => x.GetCurrentHolidayAsync());
	/// </code>
	/// </summary>
	/// <typeparam name="TInput">The type of the underlying control. E.G. Button, MenuItem, CheckBox, or a custom control.</typeparam>
	/// <typeparam name="TOutput">The result type of the expression call.</typeparam>
	/// <param name="code">The expression to run on the control.</param>
	/// <param name="timeoutMs">The maximum amount of time to give the expression to execute.</param>
	/// <returns>The result of the expression call.</returns>
	/// <exception cref="SerializationException">
	/// The result was not JSON serializable over the wire. In this case, consider returning a subset of the object, instead of the entire object.
	/// E.G.
	/// myCustomForm.InvokeAsync&lt;CustomForm, FormViewModel&gt;(x => x.GetViewModelAsync()) becomes
	/// myCustomForm.Invoke&lt;CustomForm, bool&gt;(x => x.GetHasSeenHolidayNewsAsync())
	/// Another option is not returning the result by calling the non-return version of InvokeAsync.
	/// </exception>
	/// <exception cref="TimeoutException">
	/// The InvokeAsync expression ran for too long and was cut short.
	/// </exception>
	public TOutput InvokeAsync<TInput, TOutput>(Expression<Func<TInput, Task<TOutput>>> code, int timeoutMs = 10_000)
	{
		var response = Channel.GetResponse(new
		{
			Kind = nameof(InvokeCommand),
			TargetId,
			Code = Eval.SerializeCode(code),
			TimeoutMs = timeoutMs,
		}, timeoutMs: timeoutMs);

		var responseValue = PropInfo.GetPropertyValue(response, "Value");
		if (responseValue is string s)
		{
			if (s == "UnserializableResult")
				throw new SerializationException($"Unserializable {nameof(InvokeAsync)} result received.");
			if (s == "PendingResult")
				throw new TimeoutException($"{nameof(InvokeAsync)} timeout.");
		}

		var result = (TOutput) responseValue;

		OnAction();
		return result!;
	}

	/// <summary>
	/// Executes the given async expression on the underlying control and returns the result.<br/>
	/// `await` should not be used in the code expression, this is handled by WPF Pilot.
	/// <code>
	/// ✏️ myCustomControl.InvokeAsync&lt;CustomControl, FormResult&gt;(x => x.ResetFormAsync());
	/// ✏️ calendar.InvokeAsync&lt;Calendar, DayOfWeek&gt;(x => x.GetCurrentHolidayAsync());
	/// </code>
	/// </summary>
	/// <typeparam name="TInput">The type of the underlying control. E.G. Button, MenuItem, CheckBox, or a custom control.</typeparam>
	/// <typeparam name="TOutput">The result type of the expression call.</typeparam>
	/// <param name="code">The expression to run on the control.</param>
	/// <param name="result">The output variable that holds the result of the expression call.</param>
	/// <param name="timeoutMs">The maximum amount of time to give the expression to execute.</param>
	/// <returns>The element so additional methods can be chained and the result of the expression call in the out variable.</returns>
	/// <exception cref="SerializationException">
	/// The result was not JSON serializable over the wire. In this case, consider returning a subset of the object, instead of the entire object.
	/// E.G.
	/// myCustomForm.InvokeAsync&lt;CustomForm, FormViewModel&gt;(x => x.GetViewModelAsync(), out var viewModel) becomes
	/// myCustomForm.Invoke&lt;CustomForm, bool&gt;(x => x.GetHasSeenHolidayNewsAsync(), out var hasSeenHolidayNews)
	/// Another option is not returning the result by calling the non-return version of InvokeAsync.
	/// </exception>
	/// <exception cref="TimeoutException">
	/// The InvokeAsync expression ran for too long and was cut short.
	/// </exception>
	public virtual Element InvokeAsync<TInput, TOutput>(Expression<Func<TInput, Task<TOutput>>> code, out TOutput result, int timeoutMs = 10_000)
	{
		var response = Channel.GetResponse(new
		{
			Kind = nameof(InvokeCommand),
			TargetId,
			Code = Eval.SerializeCode(code),
			TimeoutMs = timeoutMs,
		}, timeoutMs: timeoutMs);

		var responseValue = PropInfo.GetPropertyValue(response, "Value");
		if (responseValue is string s)
		{
			if (s == "UnserializableResult")
				throw new SerializationException($"Unserializable {nameof(InvokeAsync)} result received.");
			if (s == "PendingResult")
				throw new TimeoutException($"{nameof(InvokeAsync)} timeout.");
		}

		result = (TOutput) responseValue;

		OnAction();
		return this;
	}

	/// <summary>
	/// Executes the given async expression on the underlying control.<br/>
	/// `await` should not be used in the expression, this is handled by WPF Pilot.
	/// <code>
	/// ✏️ calendar.Invoke(x => x.ReleaseAllTouchCapturesAsync());
	/// </code>
	/// </summary>
	/// <param name="code">The expression to run on the control.</param>
	/// <param name="timeoutMs">The maximum amount of time to give the expression to execute.</param>
	/// <returns>The element so additional methods can be chained.</returns>
	/// <exception cref="TimeoutException">
	/// The InvokeAsync expression ran for too long and was cut short.
	/// </exception>
	public virtual Element InvokeAsync(Expression<Func<UIElement, Task>> code, int timeoutMs = 10_000)
	{
		var response = Channel.GetResponse(new
		{
			Kind = nameof(InvokeCommand),
			TargetId,
			Code = Eval.SerializeCode(code),
			TimeoutMs = timeoutMs,
		}, timeoutMs: timeoutMs);

		var responseValue = PropInfo.GetPropertyValue(response, "Value");
		if (responseValue is string s)
		{
			if (s == "PendingResult")
				throw new TimeoutException($"{nameof(InvokeAsync)} timeout.");
		}

		OnAction();
		return this;
	}

	/// <summary>
	/// Executes the given async expression on the underlying control.<br/>
	/// `await` should not be used in the expression, this is handled by WPF Pilot.
	/// <code>
	/// ✏️ calendar.Invoke&lt;Calendar&gt;(x => x.ResetCalendarAsync());
	/// </code>
	/// </summary>
	/// <param name="code">The expression to run on the control.</param>
	/// <param name="timeoutMs">The maximum amount of time to give the expression to execute.</param>
	/// <returns>The element so additional methods can be chained.</returns>
	/// <exception cref="TimeoutException">
	/// The InvokeAsync expression ran for too long and was cut short.
	/// </exception>
	public virtual Element InvokeAsync<TInput>(Expression<Func<TInput, Task>> code, int timeoutMs = 10_000)
	{
		var response = Channel.GetResponse(new
		{
			Kind = nameof(InvokeCommand),
			TargetId,
			Code = Eval.SerializeCode(code),
			TimeoutMs = timeoutMs,
		}, timeoutMs: timeoutMs);

		var responseValue = PropInfo.GetPropertyValue(response, "Value");
		if (responseValue is string s)
		{
			if (s == "PendingResult")
				throw new TimeoutException($"{nameof(InvokeAsync)} timeout.");
		}

		OnAction();
		return this;
	}

	/// <summary>
	/// <code>
	/// ✏️ element.SetProperty("IsOpen", true);
	/// </code>
	/// </summary>
	public virtual Element SetProperty(string propertyName, object? value)
	{
		var response = Channel.GetResponse(new
		{
			Kind = nameof(SetPropertyCommand),
			TargetId,
			PropertyName = propertyName,
			PropertyValue = value switch
			{
				Primitive p => WrappedArg<object>.Wrap(p.V),
				_ => WrappedArg<object>.Wrap(value),
			}
		});

		OnAction();
		return this;
	}

	/// <summary>
	/// <code>
	/// ✏️ element.SetProperty&lt;Button, bool&gt;("IsOpen", x => !x.IsOpen);
	/// </code>
	/// </summary>
	public virtual Element SetProperty<TInput, TOutput>(string propertyName, Expression<Func<TInput, TOutput>> getValue)
	{
		var response = Channel.GetResponse(new
		{
			Kind = nameof(SetPropertyCommand),
			TargetId,
			PropertyName = propertyName,
			PropertyValue = Eval.SerializeCode(getValue),
		});

		OnAction();
		return this;
	}

	/// <summary>
	/// Convenient assertion chaining method for <see cref="Element"/>.<br/>
	/// Full debug information is provided in the exception message.
	/// <code n="samples">
	/// ✏️ element.Assert(x => x["Width"] > 100).Click().Assert(x => x["Text"] == "Clicked!");
	/// </code>
	/// </summary>
	public virtual Element Assert(Expression<Func<Element, bool?>> predicateExpression, int timeoutMs = 10_000)
	{
		if (predicateExpression == null)
			throw new ArgumentNullException(nameof(predicateExpression));

		var parameter = DebugValueExpressionVisitor.GetDebugExpresssion(TypeName, this);
		var assertable = Assertable.FromValueExpression(this, parameter, OnAction);
		assertable.IsTrue(predicateExpression, timeoutMs);

		return this;
	}

	/// <summary>
	/// <code n="samples">
	/// ✏️ element.HasProperty("Text");
	/// </code>
	/// </summary>
	public bool HasProperty(string propName) =>
		Properties.ContainsKey(propName);

	/// <summary>
	/// Gets or sets the value of the specified property. Returns a `Primitive` object, which can be implicitly cast and compared to a string, bool, int, etc.<br/>
	/// <code n="samples">
	/// ✏️ var text = element["Text"];
	/// ✏️ var health = element["HealthBar"] + 5;
	/// ✏️ element["Text"] = "Hello world!";
	/// ✏️ element["IsOpen"] ? "Open popup" : "Closed popup";
	/// </code>
	/// </summary>
	public Primitive this[string propName]
	{
		get
		{
			OnAccessProperty(propName);
			return Properties.ContainsKey(propName) ? new Primitive(Properties[propName]) : Primitive.Empty;
		}
		set => SetProperty(propName, value);
	}

	internal void Refresh(string typeName, IReadOnlyDictionary<string, object> properties, string? parentId, IReadOnlyList<string> childIds)
	{
		TypeName = typeName;
		Properties = properties;
		ParentId = parentId;
		ChildIds = childIds;
	}

	private Element DoClick(string mouseButton)
	{
		var response = Channel.GetResponse(new
		{
			Kind = nameof(ClickCommand),
			TargetId,
			MouseButton = mouseButton,
		});

		OnAction();
		return this;
	}

	/// <summary>
	/// <code>
	/// ✏️ element.TypeName -> "Button", "TextBox", etc.
	/// </code>
	/// </summary>
	public string TypeName { get; private set; }

	[JsonIgnore] // Prevents infinite loop when serializing.
	public Element? Parent
	{
		get => ParentId == null ? null : TargetIdToElement[ParentId][0];
	}

	[JsonIgnore] // Prevents infinite loop when serializing.
	public IReadOnlyList<Element> Child
	{
		get => ChildIds.Select(x => TargetIdToElement[x][0]).ToList();
	}

	internal string TargetId { get; private set; } // Dynamic ID. It is not stable across runs or major tree changes.
	internal NamedPipeClient Channel { get; }
	internal Action OnAction { get; }

	private string? ParentId { get; set; }
	private IReadOnlyList<string> ChildIds { get; set; }
	private IReadOnlyDictionary<string, object> Properties { get; set; }
	private IReadOnlyDictionary<string, List<Element>> TargetIdToElement { get; set; }
	private Action<string> OnAccessProperty { get; }
}
