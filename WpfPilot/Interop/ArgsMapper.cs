#pragma warning disable

namespace WpfPilot.Interop;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Aq.ExpressionJsonSerializer;
using Newtonsoft.Json;
using WpfPilot.Utility;
using WpfPilot.Utility.WpfUtility;

internal static class ArgsMapper
{
	public static bool IsSerializable(object result)
	{
		try
		{
			var serialized = MessagePacker.Pack(WrappedArg<object>.Wrap(result));
			_ = MessagePacker.Unpack(serialized);
			return true;
		}
		catch
		{
			return false;
		}
	}

	public static dynamic[] Map(IReadOnlyList<dynamic> args)
	{
		return args.Select(MapSingle).ToArray()!;
	}

	public static dynamic? MapSingle(dynamic arg)
	{
		if (!PropInfo.HasProperty(arg, "Type"))
			return arg;

		return arg.Type switch
		{
			Eval.EvalType => MapEvalArg(arg),
			WrappedArg<object>.WrappedArgType => MapArg(arg),
			_ => arg,
		};
	}

	private static dynamic? MapEvalArg(dynamic arg)
	{
		if (!PropInfo.HasProperty(arg, "ExpressionJson"))
			throw new ArgumentException("Eval.ExpressionJson must be set.");
		if (arg.ExpressionJson is not string)
			throw new ArgumentException($"Eval.ExpressionJson must be a string.\nReceived: {Log.CreateLogString(arg.ExpressionJson)}\nType: {arg.ExpressionJson.GetType()}");

		var expression = JsonConvert.DeserializeObject<LambdaExpression>(arg.ExpressionJson, Settings);

		return expression.Compile();
	}

	private static dynamic? MapArg(dynamic arg)
	{
		if (!PropInfo.HasProperty(arg, "Value"))
			throw new ArgumentException("Value must be set.");

		return arg.Value;
	}

	private static readonly JsonSerializerSettings Settings = new()
	{
		ContractResolver = new RobustContractResolver(),
		ConstructorHandling = ConstructorHandling.AllowNonPublicDefaultConstructor,
		Converters = { new ExpressionJsonConverter(Assembly.GetExecutingAssembly()) }
	};
}
