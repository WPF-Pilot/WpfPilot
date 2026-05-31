namespace WpfPilot.Mac.Server;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

/// <summary>
/// Resolves and invokes a named method on a registered target object, mirroring the semantics of the
/// Windows WpfPilot <c>InvokeCommand</c> (reflection invoke, async-aware) without serializing an
/// expression tree. The invocation itself is marshaled onto the application main thread.
/// </summary>
internal sealed class InvocationDispatcher
{
	public InvocationDispatcher(IMainThreadInvoker mainThread)
	{
		m_mainThread = mainThread;
	}

	public async Task<object?> InvokeAsync(object target, string methodName, IReadOnlyList<object?> args, bool isAsync)
	{
		var method = ResolveMethod(target.GetType(), methodName, args.Count);
		if (method is null)
			throw new MissingMethodException($"No public method '{methodName}' with {args.Count} argument(s) on '{target.GetType().FullName}'.");

		var parameters = method.GetParameters();
		var coerced = new object?[parameters.Length];
		for (var i = 0; i < parameters.Length; i++)
			coerced[i] = Coerce(args[i], parameters[i].ParameterType);

		// Obtain (and, for async methods, start) the invocation on the app main thread, where app/UI
		// state access is legal. The returned Task is then awaited on the worker thread; the host's async
		// methods do their own dispatcher hops for any further main-thread work.
		var invocationResult = m_mainThread.Invoke<object?>(() => method.Invoke(target, coerced));

		if (!isAsync && !typeof(Task).IsAssignableFrom(method.ReturnType))
			return invocationResult;

		if (invocationResult is not Task task)
			return invocationResult;

		await task.ConfigureAwait(false);

		var resultProperty = task.GetType().GetProperty("Result");
		if (resultProperty is null)
			return null;

		// Task (non-generic) exposes no usable Result; Task<T> exposes T.
		if (resultProperty.PropertyType.FullName == "System.Threading.Tasks.VoidTaskResult")
			return null;

		return resultProperty.GetValue(task);
	}

	private static MethodInfo? ResolveMethod(Type type, string methodName, int argCount)
	{
		var candidates = type
			.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy)
			.Where(m => m.Name == methodName && m.GetParameters().Length == argCount)
			.ToList();

		// Prefer public overloads; fall back to non-public if that's all there is.
		return candidates.FirstOrDefault(m => m.IsPublic) ?? candidates.FirstOrDefault();
	}

	private static object? Coerce(object? value, Type targetType)
	{
		if (value is null)
			return null;

		var nonNullable = Nullable.GetUnderlyingType(targetType) ?? targetType;
		if (nonNullable.IsInstanceOfType(value))
			return value;

		if (nonNullable.IsEnum)
			return Enum.ToObject(nonNullable, value);

		try
		{
			return Convert.ChangeType(value, nonNullable);
		}
		catch
		{
			return value;
		}
	}

	private readonly IMainThreadInvoker m_mainThread;
}
