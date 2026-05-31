namespace WpfPilot.Mac.Driver;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using WpfPilot.Mac.Protocol;

/// <summary>
/// A handle to a remote automation target registered with the in-process <c>AutomationServer</c>.
/// Mirrors the shape of the Windows WpfPilot <c>Element</c> API (<c>Invoke</c>/<c>InvokeAsync</c>) so test
/// code reads the same on both platforms, but dispatches a method-name RPC instead of an expression tree.
/// </summary>
public sealed class Element
{
	internal Element(AutomationConnection connection, string targetId, string typeName)
	{
		m_connection = connection;
		TargetId = targetId;
		TypeName = typeName;
	}

	/// <summary>The target's registered id.</summary>
	public string TargetId { get; }

	/// <summary>The target's registered type name (e.g. <c>"E2EAutomationHost"</c>).</summary>
	public string TypeName { get; }

	/// <summary>Invokes a synchronous method on the target and returns its result.</summary>
	public TOutput Invoke<TInput, TOutput>(Expression<Func<TInput, TOutput>> code, int timeoutMs = 30_000)
	{
		var (method, args) = MethodCallParser.Parse(code);
		var value = SendInvoke(method, args, isAsync: false, timeoutMs);
		return Cast<TOutput>(value);
	}

	/// <summary>Invokes a synchronous void method on the target.</summary>
	public Element Invoke<TInput>(Expression<Action<TInput>> code, int timeoutMs = 30_000)
	{
		var (method, args) = MethodCallParser.Parse(code);
		SendInvoke(method, args, isAsync: false, timeoutMs);
		return this;
	}

	/// <summary>
	/// Invokes an async method on the target and returns the awaited result. Do not write <c>await</c> in
	/// the expression; the server awaits the returned task.
	/// </summary>
	public TOutput InvokeAsync<TInput, TOutput>(Expression<Func<TInput, Task<TOutput>>> code, int timeoutMs = 30_000)
	{
		var (method, args) = MethodCallParser.Parse(code);
		var value = SendInvoke(method, args, isAsync: true, timeoutMs);
		return Cast<TOutput>(value);
	}

	/// <summary>Invokes an async void (<see cref="Task"/>-returning) method on the target.</summary>
	public Element InvokeAsync<TInput>(Expression<Func<TInput, Task>> code, int timeoutMs = 30_000)
	{
		var (method, args) = MethodCallParser.Parse(code);
		SendInvoke(method, args, isAsync: true, timeoutMs);
		return this;
	}

	private object? SendInvoke(string method, IReadOnlyList<object?> args, bool isAsync, int timeoutMs)
	{
		var command = new CommandMessage
		{
			Kind = CommandKind.InvokeMethod,
			TargetId = TargetId,
			Method = method,
			Args = args.Select(ValueMarshal.Wrap).ToList(),
			IsAsync = isAsync,
			TimeoutMs = timeoutMs,
		};

		var response = m_connection.Send(command, timeoutMs);
		if (response.Error == "StaleElement")
			throw new InvalidOperationException("The automation target is no longer registered (stale element).");
		if (response.Error is not null)
			throw new InvalidOperationException($"Invoke '{method}' failed: {response.Error}");

		return ValueMarshal.Unwrap(response.Value);
	}

	private static TOutput Cast<TOutput>(object? value)
	{
		if (value is null)
			return default!;
		if (value is TOutput typed)
			return typed;

		var target = Nullable.GetUnderlyingType(typeof(TOutput)) ?? typeof(TOutput);
		return (TOutput) Convert.ChangeType(value, target);
	}

	private readonly AutomationConnection m_connection;
}
