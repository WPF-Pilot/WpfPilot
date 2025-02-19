﻿#if NET5_0_OR_GREATER

namespace WpfPilot;

using System;
using System.Linq.Expressions;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using System.Windows;
using WpfPilot.AppDriverPayload.Commands;
using WpfPilot.Interop;
using WpfPilot.Utility.WpfUtility;

/// <summary>
/// Use this class as a base when implementing custom elements.
/// <code>
/// public class MyCoolControl : Element&lt;MyCoolControl>
/// {
///     public MyCoolControl(Element other)
///         : base(other)
///     {
///     }
///
///     [...]
/// }
/// </code>
/// </summary>
public class Element<T> : Element
	where T : Element<T>
{
	public Element(Element other)
		: base(other)
	{
	}

	public override T Click() =>
		(T) base.Click();

	public override T RightClick() =>
		(T) base.RightClick();

	public override T DoubleClick() =>
		(T) base.DoubleClick();

	public override T Focus() =>
		(T) base.Focus();

	public override T Select() =>
		(T) base.Select();

	public override T Expand() =>
		(T) base.Expand();

	public override T Collapse() =>
		(T) base.Collapse();

	public override T SelectText(string text) =>
		(T) base.SelectText(text);

	public override T Check() =>
		(T) base.Check();

	public override T Uncheck() =>
		(T) base.Uncheck();

	public override T ScrollIntoView() =>
		(T) base.ScrollIntoView();

	public override T Type(string text) =>
		(T) base.Type(text);

	/// <inheritdoc />
	public override T Screenshot(string fileOutputPath) =>
		(T) base.Screenshot(fileOutputPath);

	/// <inheritdoc />
	public override T Screenshot(out byte[] screenshotBytes, ImageFormat format = ImageFormat.Jpeg) =>
		(T) base.Screenshot(out screenshotBytes, format);

	/// <inheritdoc />
	public override T RaiseEvent<TInput>(Expression<Func<TInput, RoutedEventArgs>> code) =>
		(T) base.RaiseEvent<TInput>(code);

	/// <inheritdoc />
	public new T Invoke<TInput, TOutput>(Expression<Func<TInput, TOutput>> code, out TOutput? result, int timeoutMs = 10_000)
	{
		var response = Channel.GetResponse(new
		{
			Kind = nameof(InvokeCommand),
			TargetId,
			Code = Eval.SerializeCode(code),
			TimeoutMs = timeoutMs
		}, timeoutMs);

		var responseValue = PropInfo.GetPropertyValue(response, "Value");
		if (responseValue is string s)
		{
			if (s == "UnserializableResult")
				throw new SerializationException($"{nameof(Invoke)} result is not serializable.");
			if (s == "PendingResult")
				throw new TimeoutException($"{nameof(Invoke)} timeout.");
		}

		result = responseValue;

		OnAction();
		return (T) this;
	}

	/// <inheritdoc />
	public override T Invoke<TInput>(Expression<Action<TInput>> code, int timeoutMs = 10_000) =>
		(T) base.Invoke(code, timeoutMs);

	/// <inheritdoc />
	public new T InvokeAsync<TInput, TOutput>(Expression<Func<TInput, Task<TOutput>>> code, out TOutput result, int timeoutMs = 10_000)
	{
		var response = Channel.GetResponse(new
		{
			Kind = nameof(InvokeCommand),
			TargetId,
			Code = Eval.SerializeCode(code),
			TimeoutMs = timeoutMs
		}, timeoutMs);

		var responseValue = PropInfo.GetPropertyValue(response, "Value");
		if (responseValue is string s)
		{
			if (s == "UnserializableResult")
				throw new SerializationException($"{nameof(Invoke)} result is not serializable.");
			if (s == "PendingResult")
				throw new TimeoutException($"{nameof(Invoke)} timeout.");
		}

		result = (TOutput) responseValue;

		OnAction();
		return (T) this;
	}

	/// <inheritdoc />
	public override T InvokeAsync<TInput>(Expression<Func<TInput, Task>> code, int timeoutMs = 10_000) =>
		(T) base.InvokeAsync(code, timeoutMs);

	/// <inheritdoc />
	public override T Assert(Expression<Func<Element, bool?>> predicateExpression, int timeoutMs = 10_000) =>
		(T) base.Assert(predicateExpression, timeoutMs);

	/// <inheritdoc />
	public override T SetProperty(string propertyName, object? value) =>
		(T) base.SetProperty(propertyName, value);

	/// <inheritdoc />
	public override T SetProperty<TInput, TOutput>(string propertyName, Expression<Func<TInput, TOutput>> getValue) =>
		(T) base.SetProperty(propertyName, getValue);
}
#endif