namespace WpfPilot.Utility.WpfUtility;

using System;
using System.ComponentModel;
using System.Reflection;
using System.Runtime.CompilerServices;

internal class BindableType : INotifyPropertyChanged
{
	public BindableType(Type type)
	{
		this.Type = type;
	}

	public Type Type { get; }

	public Type UnderlyingSystemType => this.Type.UnderlyingSystemType;

	public string Name => this.Type.Name;

	public Guid GUID => this.Type.GUID;

	public Module Module => this.Type.Module;

	public Assembly Assembly => this.Type.Assembly;

	public string? FullName => this.Type.FullName;

	public string? Namespace => this.Type.Namespace;

	public string? AssemblyQualifiedName => this.Type.AssemblyQualifiedName;

	public Type? BaseType => this.Type.BaseType;

	public bool IsEnum => this.Type.IsEnum;

	public bool IsGenericType => this.Type.IsGenericType;

	public bool IsValueType => this.Type.IsValueType;

	public event PropertyChangedEventHandler? PropertyChanged;

	public static implicit operator BindableType?(Type? type)
	{
		return ToBindableType(type);
	}

	public static implicit operator Type?(BindableType? type)
	{
		return ToType(type);
	}

	public static BindableType? FromType(Type? type)
	{
		return ToBindableType(type);
	}

	public static BindableType? ToBindableType(Type? type)
	{
		if (type is null)
		{
			return null;
		}

		return new(type);
	}

	public static Type? FromBindableType(BindableType? type)
	{
		return ToType(type);
	}

	public static Type? ToType(BindableType? type)
	{
		return type?.Type;
	}

	protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
	{
		this.PropertyChanged?.Invoke(this, new(propertyName));
	}

	public object[] GetCustomAttributes(Type attributeType, bool inherit)
	{
		return this.Type.GetCustomAttributes(attributeType, inherit);
	}
}