namespace WpfPilot.Utility.WpfUtility.Diagnostics.Providers;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Reflection;
using WpfPilot.Utility.WpfUtility.Diagnostics;

internal class BindingLeakDiagnosticProvider : DiagnosticProvider
{
	private static readonly Type? reflectTypeDescriptionProviderType = typeof(TypeDescriptionProvider).Module.GetType("System.ComponentModel.ReflectTypeDescriptionProvider");

	private static readonly FieldInfo? propertyCacheFieldInfo = reflectTypeDescriptionProviderType?.GetField("_propertyCache", BindingFlags.Static | BindingFlags.NonPublic)
																?? reflectTypeDescriptionProviderType?.GetField("s_propertyCache", BindingFlags.Static | BindingFlags.NonPublic);

	private static readonly FieldInfo? valueChangedHandlersFieldInfo = typeof(PropertyDescriptor).GetField("valueChangedHandlers", BindingFlags.Instance | BindingFlags.NonPublic)
																	   ?? typeof(PropertyDescriptor).GetField("_valueChangedHandlers", BindingFlags.Instance | BindingFlags.NonPublic);

	public override string Name { get; } = "Binding leak";

	public override string Description { get; } = "Detects binding leaks.";

	protected override IEnumerable<DiagnosticItem> GetGlobalDiagnosticItemsInternal()
	{
		var reflectPropertyDescriptorInfos = GetReflectPropertyDescriptorInfo();

		if (reflectPropertyDescriptorInfos is null)
		{
			yield break;
		}

		foreach (var descriptorInfo in reflectPropertyDescriptorInfos)
		{
			yield return
				new DiagnosticItem(this,
					"Binding leak",
					$"Property '{descriptorInfo.PropertyName}' from type '{descriptorInfo.TypeName}' is bound {descriptorInfo.ValueChangedHandlers.Count} times causing binding leaks.",
					DiagnosticArea.Binding,
					DiagnosticLevel.Warning);
		}
	}

	// Code idea, for looking into ReflectTypeDescriptionProvider, taken from https://faithlife.codes/blog/2008/10/detecting_bindings_that_should_be_onetime/
	// Credit goes to Bradley Grainger (https://github.com/bgrainger)
	private static ReadOnlyCollection<ReflectPropertyDescriptorInfo>? GetReflectPropertyDescriptorInfo()
	{
		if (propertyCacheFieldInfo is null
			|| valueChangedHandlersFieldInfo is null)
		{
			return null;
		}

		var propertyCache = (Hashtable?) propertyCacheFieldInfo.GetValue(null);

		if (propertyCache is null)
		{
			return null;
		}

		List<ReflectPropertyDescriptorInfo> listInfo = new();

		// try to make a copy of the hashtable as quickly as possible (this object can be accessed by other threads)

		DictionaryEntry[] entries = new DictionaryEntry[propertyCache.Count];
		propertyCache.CopyTo(entries, 0);

		// count the "value changed" handlers for each type
		foreach (var entry in entries)
		{
			var pds = (PropertyDescriptor[]?) entry.Value;
			if (pds is null)
			{
				continue;
			}

			foreach (var pd in pds)
			{
				var valueChangedHandlers = (IDictionary?) valueChangedHandlersFieldInfo.GetValue(pd);
				if (valueChangedHandlers is not null
					&& valueChangedHandlers.Count != 0)
				{
					listInfo.Add(new ReflectPropertyDescriptorInfo(entry.Key.ToString()!, pd.Name, valueChangedHandlers!));
				}
			}
		}

		listInfo.Sort();
		return listInfo.AsReadOnly();
	}

	// Code idea, for looking into ReflectTypeDescriptionProvider, taken from https://faithlife.codes/blog/2008/10/detecting_bindings_that_should_be_onetime/
	// Credit goes to Bradley Grainger (https://github.com/bgrainger)
	public sealed class ReflectPropertyDescriptorInfo : IEquatable<ReflectPropertyDescriptorInfo>, IComparable<ReflectPropertyDescriptorInfo>
	{
		public ReflectPropertyDescriptorInfo(string typeName, string propertyName, IDictionary valueChangedHandlers)
		{
			this.TypeName = typeName;
			this.PropertyName = propertyName;
			this.ValueChangedHandlers = valueChangedHandlers;
		}

		public string TypeName { get; }

		public string PropertyName { get; }

		public IDictionary ValueChangedHandlers { get; }

		public int CompareTo(ReflectPropertyDescriptorInfo? other)
		{
			if (other is null)
			{
				return 1;
			}

			var compareResult = string.Compare(this.TypeName, other.TypeName, StringComparison.Ordinal);
			if (compareResult == 0)
			{
				compareResult = string.Compare(this.PropertyName, other.PropertyName, StringComparison.Ordinal);
			}

			if (compareResult == 0)
			{
				compareResult = this.ValueChangedHandlers.Count.CompareTo(other.ValueChangedHandlers.Count);
			}

			return compareResult;
		}

		public bool Equals(ReflectPropertyDescriptorInfo? other)
		{
			if (other is null)
			{
				return false;
			}

			if (ReferenceEquals(this, other))
			{
				return true;
			}

			return this.TypeName == other.TypeName
				   && this.PropertyName == other.PropertyName
				   && this.ValueChangedHandlers == other.ValueChangedHandlers;
		}

		public override bool Equals(object? obj)
		{
			return ReferenceEquals(this, obj)
				   || (obj is ReflectPropertyDescriptorInfo other && this.Equals(other));
		}

#if NETCOREAPP
		public override int GetHashCode()
		{
			return HashCode.Combine(this.TypeName, this.PropertyName);
		}
#else
		public override int GetHashCode()
		{
			unchecked
			{
				var hashCode = this.TypeName.GetHashCode();
				hashCode = hashCode * 397 ^ this.PropertyName.GetHashCode();
				return hashCode;
			}
		}
#endif
	}
}