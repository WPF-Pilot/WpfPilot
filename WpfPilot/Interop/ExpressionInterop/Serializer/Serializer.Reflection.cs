namespace Aq.ExpressionJsonSerializer;

using System;
using System.Collections.Generic;
using System.Reflection;

internal partial class Serializer
{
	private static readonly Dictionary<Type, Tuple<string, string, Type[]>>
		TypeCache = new Dictionary<Type, Tuple<string, string, Type[]>>();

	private Action Type(Type type)
	{
		return () => TypeInternal(type);
	}

	private void TypeInternal(Type type)
	{
		if (type == null)
		{
			Writer.WriteNull();
		}
		else
		{
			Tuple<string, string, Type[]>? tuple;
			if (!TypeCache.TryGetValue(type, out tuple))
			{
				var assemblyName = type.Assembly.FullName;
				if (type.IsGenericType)
				{
					var def = type.GetGenericTypeDefinition();
					tuple = new Tuple<string, string, Type[]>(
						def.Assembly.FullName, def.FullName,
						type.GetGenericArguments()
					);
				}
				else
				{
					tuple = new Tuple<string, string, Type[]>(assemblyName, type.FullName, null!);
				}

				TypeCache[type] = tuple;
			}

			Writer.WriteStartObject();
			Prop("assemblyName", tuple.Item1);
			Prop("typeName", tuple.Item2);
			Prop("genericArguments", Enumerable(tuple.Item3, Type));
			Writer.WriteEndObject();
		}
	}

	private Action Constructor(ConstructorInfo constructor)
	{
		return () => ConstructorInternal(constructor);
	}

	private void ConstructorInternal(ConstructorInfo constructor)
	{
		if (constructor == null)
		{
			Writer.WriteNull();
		}
		else
		{
			Writer.WriteStartObject();
			Prop("type", Type(constructor.DeclaringType));
			Prop("name", constructor.Name);
			Prop("signature", constructor.ToString());
			Writer.WriteEndObject();
		}
	}

	private Action Method(MethodInfo method)
	{
		return () => MethodInternal(method);
	}

	private void MethodInternal(MethodInfo method)
	{
		if (method == null)
		{
			Writer.WriteNull();
		}
		else
		{
			Writer.WriteStartObject();
			if (method.IsGenericMethod)
			{
				var meth = method.GetGenericMethodDefinition();
				var generic = method.GetGenericArguments();

				Prop("type", Type(meth.DeclaringType));
				Prop("name", meth.Name);
				Prop("signature", meth.ToString());
				Prop("generic", Enumerable(generic, Type));
			}
			else
			{
				Prop("type", Type(method.DeclaringType));
				Prop("name", method.Name);
				Prop("signature", method.ToString());
			}

			Writer.WriteEndObject();
		}
	}

	private Action Property(PropertyInfo property)
	{
		return () => PropertyInternal(property);
	}

	private void PropertyInternal(PropertyInfo property)
	{
		if (property == null)
		{
			Writer.WriteNull();
		}
		else
		{
			Writer.WriteStartObject();
			Prop("type", Type(property.DeclaringType));
			Prop("name", property.Name);
			Prop("signature", property.ToString());
			Writer.WriteEndObject();
		}
	}

	private Action Member(MemberInfo member)
	{
		return () => MemberInternal(member);
	}

	private void MemberInternal(MemberInfo member)
	{
		if (member == null)
		{
			Writer.WriteNull();
		}
		else
		{
			Writer.WriteStartObject();
			Prop("type", Type(member.DeclaringType));
			Prop("memberType", (int) member.MemberType);
			Prop("name", member.Name);
			Prop("signature", member.ToString());
			Writer.WriteEndObject();
		}
	}
}
