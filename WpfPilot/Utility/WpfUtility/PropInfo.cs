// (c) Copyright Cory Plotts.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

namespace WpfPilot.Utility.WpfUtility;

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Dynamic;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using WpfPilot.Utility;
using WpfPilot.Utility.WpfUtility.Converters;
using WpfPilot.Utility.WpfUtility.Helpers;

internal class PropInfo : DependencyObject, IComparable, INotifyPropertyChanged
{
	private static readonly Attribute[] getAllPropertiesAttributeFilter = { PropertyFilterAttribute.Default };

	private readonly object? component;
	private bool breakOnChange;
	private bool hasChangedRecently;
	private ValueSource valueSource;

	private readonly PropertyDescriptor? property;
	private bool wasTriedAsDependencyProperty;
	private DependencyProperty? dependencyProperty;
	private readonly string? name;
	private readonly string displayName;
	private bool isLocallySet;

	private bool isInvalidBinding;
	private int index;

	private bool isDatabound;

	private bool isRunning;

	private string bindingError = string.Empty;

	/// <summary>
	/// Normal constructor used when constructing PropInfo objects for properties.
	/// </summary>
	/// <param name="target">target object being shown in the property grid</param>
	/// <param name="property">the property around which we are constructing this PropInfo object</param>
	/// <param name="propertyName">the property name for the property that we use in the binding in the case of a non-dependency property</param>
	/// <param name="propertyDisplayName">the display name for the property that goes in the name column</param>
	public PropInfo(object target, PropertyDescriptor? property, string propertyName, string propertyDisplayName)
	{
		this.Target = target;
		this.property = property;
		this.name = propertyName;
		this.displayName = propertyDisplayName;

		if (property is not null)
		{
			// create a data binding between the actual property value on the target object
			// and the Value dependency property on this PropInfo object
			Binding binding;
			var dp = this.DependencyProperty;
			if (dp is not null)
			{
				binding = new Binding
				{
					Path = new PropertyPath("(0)", dp)
				};

				if (dp == FrameworkElement.StyleProperty
					|| dp == FrameworkContentElement.StyleProperty)
				{
					binding.Converter = NullStyleConverter.DefaultInstance;
					binding.ConverterParameter = target;
				}
			}
			else
			{
				binding = new Binding(propertyName);
			}

			binding.Source = target;
			binding.Mode = GetBindingMode();

			try
			{
				BindingOperations.SetBinding(this, ValueProperty, binding);
			}
			catch (Exception)
			{
				// cplotts note:
				// warning: i saw a problem get swallowed by this empty catch (Exception) block.
				// in other words, this empty catch block could be hiding some potential future errors.
			}
		}

		this.Update();

		this.isRunning = true;

		BindingMode GetBindingMode()
		{
			return property.IsReadOnly ? BindingMode.OneWay : BindingMode.TwoWay;
		}
	}

	/// <summary>
	/// Constructor used when constructing PropInfo objects for an item in a collection.
	/// In this case, we set the PropertyDescriptor for this object (in the property Property) to be null.
	/// This kind of makes since because an item in a collection really isn't a property on a class.
	/// That is, in this case, we're really hijacking the PropInfo class
	/// in order to expose the items in the Snoop property grid.
	/// </summary>
	/// <param name="target">the item in the collection</param>
	/// <param name="component">the collection</param>
	/// <param name="displayName">the display name that goes in the name column, i.e. this[x]</param>
	/// <param name="value">the value</param>
	public PropInfo(object target, object? component, string displayName, object? value)
		: this(target, null, displayName, displayName)
	{
		this.component = component;
		this.isRunning = false;
		this.Value = value;
		this.isRunning = true;
	}

	public void Teardown()
	{
		this.isRunning = false;
		BindingOperations.ClearAllBindings(this);
	}

	public object? Target { get; }

	public object? Value
	{
		get => this.GetValue(ValueProperty);
		set => this.SetValue(ValueProperty, value);
	}

	public static readonly DependencyProperty ValueProperty =
		DependencyProperty.Register(
			nameof(Value),
			typeof(object),
			typeof(PropInfo),
			new PropertyMetadata(OnValueChanged));

	private static void OnValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
	{
		((PropInfo) d).OnValueChanged(e);
	}

	protected virtual void OnValueChanged(DependencyPropertyChangedEventArgs e)
	{
		if (this.isRunning == false)
		{
			return;
		}

		this.Update();

		if (this.breakOnChange)
		{
			if (Debugger.IsAttached == false)
			{
				Debugger.Launch();
			}

			Debugger.Break();
		}

		this.HasChangedRecently = (e.OldValue?.Equals(e.NewValue) ?? e.OldValue == e.NewValue) == false;

		if (this.changeTimer is null)
		{
			this.changeTimer = new DispatcherTimer
			{
				Interval = TimeSpan.FromSeconds(1.5)
			};
			this.changeTimer.Tick += this.HandleChangeExpiry;
			this.changeTimer.Start();
		}
		else
		{
			this.changeTimer.Stop();
			this.changeTimer.Start();
		}
	}

	private void HandleChangeExpiry(object? sender, EventArgs e)
	{
		this.changeTimer?.Stop();
		this.changeTimer = null;

		this.HasChangedRecently = false;
	}

	private DispatcherTimer? changeTimer;

	public string StringValue
	{
		get
		{
			var value = this.Value;
			if (value is not null)
			{
				return value.ToString() ?? string.Empty;
			}

			return string.Empty;
		}

		set
		{
#pragma warning disable WPF0036 // Avoid side effects in CLR accessors.
			if (this.property is null)
			{
				// if this is a PropInfo object constructed for an item in a collection
				// then just return, since setting the value via a string doesn't make sense.
				return;
			}
#pragma warning restore WPF0036 // Avoid side effects in CLR accessors.

			var targetType = this.property.PropertyType;

			try
			{
				this.property.SetValue(this.Target, StringValueConverter.ConvertFromString(targetType, value));
			}
			catch
			{
			}
		}
	}

	public object? ResourceKey
	{
		get
		{
			var value = this.Value;
			if (value is null
				|| this.property is null)
			{
				return null;
			}

			switch (value)
			{
				case DynamicResourceExtension { ResourceKey: { } } dynamicResourceExtension:
					return dynamicResourceExtension.ResourceKey.ToString();

				case StaticResourceExtension { ResourceKey: { } } staticResourceExtension:
					return staticResourceExtension.ResourceKey.ToString();

				default:
					{
						if (this.Target is DependencyObject dependencyObject)
						{
							// Cache the resource key for this item if not cached already. This could be done for more types, but would need to optimize perf.
							if (TypeMightHaveResourceKey(this.property.PropertyType))
							{
								var resourceKey = ResourceKeyCache.Instance.GetOrAddKey(dependencyObject, value);

								return resourceKey;
							}
						}

						break;
					}
			}

			return null;
		}
	}

	public string DescriptiveValue
	{
		get
		{
			var value = this.Value;
			if (value is null)
			{
				return string.Empty;
			}

			var stringValue = value.ToString() ?? string.Empty;

			var stringValueIsTypeToString = stringValue.Equals(value.GetType().ToString(), StringComparison.Ordinal);

			// Add brackets around types to distinguish them from values.
			// Replace long type names with short type names for some specific types, for easier readability.
			// FUTURE: This could be extended to other types.
			if (stringValueIsTypeToString)
			{
				switch (value)
				{
					case DynamicResourceExtension dynamicResourceExtension:
						return $"[DynamicResource] {dynamicResourceExtension.ResourceKey}";

					case StaticResourceExtension staticResourceExtension:
						return $"[StaticResource] {staticResourceExtension.ResourceKey}";

					case ResourceDictionary { Source: { } } rd:
						return $"[ResourceDictionary] {rd.Source}";
				}

				// Try to use a short type name
				stringValue = ShouldWeDisplayShortTypeName(value.GetType())
					? $"[{value.GetType().Name}]"
					: $"[{stringValue}]";
			}

			// Display #00FFFFFF as Transparent for easier readability
			if (this.property is not null
				&& this.property.PropertyType == typeof(Brush)
				&& stringValue.Equals("#00FFFFFF", StringComparison.Ordinal))
			{
				stringValue = "Transparent";
			}

			{
				// Display both the value and the resource key, if there's a key for this property.
				if (ResourceKeyHelper.IsValidResourceKey(this.ResourceKey))
				{
					return $"{stringValue} [{this.ResourceKey}]";
				}

				// if the value comes from a Binding, show the binding details in [] brackets
				if (this.IsExpression
					&& this.Binding is { } binding)
				{
					var bindingDescriptiveString = BindingDisplayHelper.BuildBindingDescriptiveString(binding);

					if (stringValueIsTypeToString)
					{
						return $"[Binding] {bindingDescriptiveString}";
					}

					return $"{stringValue} [Binding] {bindingDescriptiveString}";
				}
			}

			if (value is Setter setter)
			{
				stringValue = "Setter ";

				if (setter.Property is not null)
				{
					stringValue += $"Property: {setter.Property.Name}";

					if (string.IsNullOrEmpty(setter.TargetName) == false)
					{
						stringValue += "; ";
					}
				}

				if (string.IsNullOrEmpty(setter.TargetName) == false)
				{
					stringValue += $"Target: {setter.TargetName}";
				}
			}

			return stringValue;
		}
	}

	private static bool ShouldWeDisplayShortTypeName(Type type)
	{
		return TypeMightHaveResourceKey(type);
	}

	private static bool TypeMightHaveResourceKey(Type type)
	{
		return typeof(Brush).IsAssignableFrom(type)
			   || typeof(Color).IsAssignableFrom(type)
			   || typeof(ControlTemplate).IsAssignableFrom(type)
			   || typeof(DataTemplate).IsAssignableFrom(type)
			   || typeof(DrawingImage).IsAssignableFrom(type)
			   || typeof(Storyboard).IsAssignableFrom(type)
			   || typeof(Style).IsAssignableFrom(type)

			   || typeof(DataTemplateSelector).IsAssignableFrom(type)
			   || typeof(ItemContainerTemplateSelector).IsAssignableFrom(type)
			   || typeof(StyleSelector).IsAssignableFrom(type)

			   || typeof(IValueConverter).IsAssignableFrom(type)
			   || typeof(IMultiValueConverter).IsAssignableFrom(type);
	}

	public BindableType? ComponentType
	{
		get
		{
			if (this.property is null)
			{
				// if this is a PropInfo object constructed for an item in a collection
				// then this.property will be null, but this.component will contain the collection.
				// use this object to return the type of the collection for the ComponentType.
				return this.component?.GetType();
			}

			return this.property.ComponentType;
		}
	}

	public string BindingError
	{
		get => this.bindingError;
		private set
		{
			if (value == this.bindingError)
			{
				return;
			}

			this.bindingError = value;
			this.OnPropertyChanged(nameof(this.BindingError));
		}
	}

	public string? Name => this.name;

	public string DisplayName => this.displayName;

	public bool IsCollectionEntry { get; private set; }

	public object? CollectionEntryIndexOrKey { get; private set; }

	public bool IsInvalidBinding => this.isInvalidBinding;

	public bool IsLocallySet => this.isLocallySet;

	public bool IsDatabound => this.isDatabound;

	public bool IsExpression => this.valueSource.IsExpression || this.Binding is not null;

	public bool IsAnimated => this.valueSource.IsAnimated;

	public int Index
	{
		get => this.index;

		set
		{
			if (this.index != value)
			{
				this.index = value;
				this.OnPropertyChanged(nameof(this.Index));
			}
		}
	}

	public BindingBase? Binding
	{
		get
		{
			var dp = this.DependencyProperty;
			if (dp is not null
				&& this.Target is DependencyObject d)
			{
				return BindingOperations.GetBindingBase(d, dp);
			}

			return this.Value as BindingBase;
		}
	}

	public bool BreakOnChange
	{
		get => this.breakOnChange;

		set
		{
			this.breakOnChange = value;
			this.OnPropertyChanged(nameof(this.BreakOnChange));
		}
	}

	public bool HasChangedRecently
	{
		get => this.hasChangedRecently;

		set
		{
			this.hasChangedRecently = value;
			this.OnPropertyChanged(nameof(this.HasChangedRecently));
		}
	}

	public ValueSource ValueSource => this.valueSource;

	/// <summary>
	/// Returns the DependencyProperty identifier for the property that this PropInfo wraps.
	/// If the wrapped property is not a DependencyProperty, null is returned.
	/// </summary>
	public DependencyProperty? DependencyProperty
	{
		get
		{
			if (this.dependencyProperty is not null)
			{
				return this.dependencyProperty;
			}

			if (this.property is not null
				&& this.wasTriedAsDependencyProperty == false)
			{
				this.wasTriedAsDependencyProperty = true;
				this.dependencyProperty = DependencyPropertyDescriptor.FromProperty(this.property)?.DependencyProperty;

				return this.dependencyProperty;
			}

			return null;
		}
	}

	private void Update()
	{
		this.isLocallySet = false;
		this.isInvalidBinding = false;
		this.BindingError = string.Empty;
		this.isDatabound = false;

		var dp = this.DependencyProperty;
		var d = this.Target as DependencyObject;

		if (dp is not null
			&& d is not null)
		{
			//Debugger.Launch();
			if (d.ReadLocalValue(dp) != DependencyProperty.UnsetValue)
			{
				this.isLocallySet = true;
			}

			var expression = BindingOperations.GetBindingExpressionBase(d, dp);
			if (expression is not null)
			{
				this.isDatabound = true;

				if (expression.HasError
					|| (expression.Status != BindingStatus.Active && expression is not PriorityBindingExpression))
				{
					this.isInvalidBinding = true;
				}
			}

			this.valueSource = DependencyPropertyHelper.GetValueSource(d, dp);
		}

		this.OnPropertyChanged(nameof(this.IsLocallySet));
		this.OnPropertyChanged(nameof(this.IsInvalidBinding));
		this.OnPropertyChanged(nameof(this.BindingError));
		this.OnPropertyChanged(nameof(this.StringValue));
		this.OnPropertyChanged(nameof(this.ResourceKey));
		this.OnPropertyChanged(nameof(this.DescriptiveValue));
		this.OnPropertyChanged(nameof(this.IsDatabound));
		this.OnPropertyChanged(nameof(this.IsExpression));
		this.OnPropertyChanged(nameof(this.IsAnimated));
		this.OnPropertyChanged(nameof(this.ValueSource));
	}

	public static List<PropInfo> GetProperties(object? obj, HashSet<string> propNames)
	{
		if (obj is null)
			return new List<PropInfo>();

		var properties = new List<PropInfo>();
		var propertyDescriptors = GetAllProperties(obj, getAllPropertiesAttributeFilter);

		// filter the properties
		foreach (var property in propertyDescriptors)
		{
			if (propNames.Contains(property.Name))
			{
				try
				{
					var prop = new PropInfo(obj, property, property.Name, property.DisplayName);
					properties.Add(prop);
				}
				catch (Exception e)
				{
					Log.Error($"Failed to create PropInfo for property '{property.Name}' on '{obj}'.{Environment.NewLine}{e}");
				}
			}
		}

		if (obj is FrameworkElement or FrameworkContentElement)
		{
			{
				const string propertyName = "DefaultStyleKey";
				if (obj.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic) is not null)
				{
					properties.Add(new(obj, TypeDescriptor.CreateProperty(obj.GetType(), propertyName, typeof(Style)), propertyName, propertyName));
				}
			}
		}

		//delve path. also, issue 4919
		var extendedProps = GetExtendedProperties(obj);
		if (extendedProps is not null)
			properties.InsertRange(0, extendedProps);

		if (obj is ResourceDictionary resourceDictionary) // if the object is a ResourceDictionary, add the items in the collection as properties
		{
			foreach (var key in resourceDictionary.Keys)
			{
				resourceDictionary.TryGetValue(key, out var item, out var exception);

				if (item is not null || key is not null)
				{
					var value = exception?.ToString() ?? item;
					var info = new PropInfo(value ?? key!, resourceDictionary, "this[" + key + "]", value)
					{
						IsCollectionEntry = true,
						CollectionEntryIndexOrKey = key
					};

					properties.Add(info);
				}
			}
		}
		else if (obj is ICollection collection) // if the object is a collection, add the items in the collection as properties
		{
			var index = 0;
			foreach (var item in collection)
			{
				if (item is null)
					continue;

				var info = new PropInfo(item, collection, "this[" + index + "]", item)
				{
					IsCollectionEntry = true,
					CollectionEntryIndexOrKey = index
				};

				index++;
				properties.Add(info);
			}
		}

		return properties;
	}

	private static IList<PropInfo>? GetExtendedProperties(object? obj)
	{
		if (obj is null)
			return null;

		{
			var key = ResourceKeyCache.Instance.GetKey(obj);

			if (ResourceKeyHelper.IsValidResourceKey(key))
			{
				var prop = new PropInfo(key!, null, "x:Key", key!);
				return new List<PropInfo>
				{
					prop
				};
			}
		}

		if (obj is string || obj.GetType().IsValueType)
			return new List<PropInfo> { new(obj, null, "ToString", obj) };

		if (obj is AutomationPeer automationPeer)
		{
			var automationProperties = new List<PropInfo>
			{
				new(obj, null, "ClassName", automationPeer.GetClassName()),
				new(obj, null, nameof(Name), automationPeer.GetName()),
				new(obj, null, "AcceleratorKey", automationPeer.GetAcceleratorKey()),
				new(obj, null, "AccessKey", automationPeer.GetAccessKey()),
				new(obj, null, "AutomationControlType", automationPeer.GetAutomationControlType()),
				new(obj, null, "AutomationId", automationPeer.GetAutomationId()),
				new(obj, null, "BoundingRectangle", automationPeer.GetBoundingRectangle()),
				new(obj, null, "ClickablePoint", automationPeer.GetClickablePoint()),
				new(obj, null, "HelpText", automationPeer.GetHelpText()),
				new(obj, null, "ItemStatus", automationPeer.GetItemStatus()),
				new(obj, null, "ItemType", automationPeer.GetItemType()),
				new(obj, null, "LabeledBy", automationPeer.GetLabeledBy()),
				new(obj, null, "LocalizedControlType", automationPeer.GetLocalizedControlType()),
				new(obj, null, "Orientation", automationPeer.GetOrientation()),
			};

			var supportedPatterns = new List<string>();

			foreach (var patternInterface in Enum.GetValues(typeof(PatternInterface)).Cast<PatternInterface>())
			{
				if (automationPeer.GetPattern(patternInterface) is not null)
				{
					supportedPatterns.Add(patternInterface.ToString());
				}
			}

			automationProperties.Add(new PropInfo(obj, null, "SupportedPatterns", string.Join(", ", supportedPatterns)));

			return automationProperties;
		}

		return null;
	}

	public static HashSet<PropertyDescriptor> GetAllProperties(object obj, Attribute[] attributes)
	{
		var propertiesToReturn = new HashSet<PropertyDescriptor>();

		object? currentObj = obj;

		// keep looping until you don't have an AmbiguousMatchException exception
		// and you normally won't have an exception, so the loop will typically execute only once.
		var noException = false;
		while (!noException && currentObj is not null)
		{
			try
			{
				// try to get the properties using the GetProperties method that takes an instance
				var properties = TypeDescriptor.GetProperties(currentObj, attributes);
				noException = true;

				MergeProperties(properties, propertiesToReturn);
			}
			catch (AmbiguousMatchException)
			{
				// if we get an AmbiguousMatchException, the user has probably declared a property that hides a property in an ancestor
				// see issue 6258 (http://snoopwpf.codeplex.com/workitem/6258)
				//
				// public class MyButton : Button
				// {
				//     public new double? Width
				//     {
				//         get { return base.Width; }
				//         set { base.Width = value.Value; }
				//     }
				// }

				var t = currentObj.GetType();
				var properties = TypeDescriptor.GetProperties(t, attributes);

				MergeProperties(properties, propertiesToReturn);

				var nextBaseTypeWithDefaultConstructor = GetNextTypeWithDefaultConstructor(t);

				if (nextBaseTypeWithDefaultConstructor is null)
					break;

				currentObj = Activator.CreateInstance(nextBaseTypeWithDefaultConstructor);
			}
		}

		return propertiesToReturn;
	}

	public static bool HasDefaultConstructor(Type? type)
	{
		var constructors = type?.GetConstructors();

		if (constructors is null)
		{
			return false;
		}

		foreach (var constructor in constructors)
		{
			if (constructor.GetParameters().Length == 0)
			{
				return true;
			}
		}

		return false;
	}

	public static Type? GetNextTypeWithDefaultConstructor(Type type)
	{
		var t = type.BaseType;

		while (!HasDefaultConstructor(t))
		{
			t = t?.BaseType;
		}

		return t;
	}

	public static bool HasProperty(object obj, string propertyName)
	{
		if (obj is null)
			return false;

		if (obj is ExpandoObject)
			return ((IDictionary<string, object>) obj).ContainsKey(propertyName);

		return obj.GetType().GetProperty(propertyName) != null;
	}

	public static object? GetPropertyValue(object obj, string propertyName)
	{
		if (obj is null)
			return null;

		if (obj is ExpandoObject)
		{
			((IDictionary<string, object>) obj).TryGetValue(propertyName, out var result);
			return result;
		}

		return obj.GetType().GetProperty(propertyName)?.GetValue(obj);
	}

	private static void MergeProperties(IEnumerable newProperties, HashSet<PropertyDescriptor> allProperties)
	{
		foreach (var newProperty in newProperties)
		{
			if (newProperty is not PropertyDescriptor newPropertyDescriptor)
				continue;

			allProperties.Add(newPropertyDescriptor);
		}
	}

	private int CollectionIndex()
	{
		if (this.IsCollectionEntry
			&& this.CollectionEntryIndexOrKey is int collectionEntryIndex)
		{
			return collectionEntryIndex;
		}

		return -1;
	}

	// Used by `.Sort`.
	public int CompareTo(object? obj)
	{
		var other = obj as PropInfo;

		if (other is not null)
		{
			var thisIndex = this.CollectionIndex();
			var otherIndex = other.CollectionIndex();

			if (thisIndex >= 0
				&& otherIndex >= 0)
			{
				return thisIndex.CompareTo(otherIndex);
			}
		}

		return string.Compare(this.DisplayName, other?.DisplayName, StringComparison.Ordinal);
	}

	public event PropertyChangedEventHandler? PropertyChanged;

	protected void OnPropertyChanged(string propertyName)
	{
		this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
	}
}