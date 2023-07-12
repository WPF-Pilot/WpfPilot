namespace WpfPilot.Utility.WpfUtility.Helpers;

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Data;
using System.Windows.Markup;
using System.Xml;
using System.Xml.Linq;

internal static class XamlWriterHelper
{
	public static XElement GetXamlAsXElement(object obj)
	{
		var xaml = GetXamlAsString(obj);

		return XElement.Parse(xaml);
	}

	public static string GetXamlAsString(object? obj)
	{
		if (obj is null)
		{
			return string.Empty;
		}

		var writerSettings = new XmlWriterSettings
		{
			Indent = true,
			OmitXmlDeclaration = true,
			NamespaceHandling = NamespaceHandling.OmitDuplicates,
			Encoding = Encoding.UTF8
		};

		var xamlString = new StringBuilder();
		var xamlDesignerSerializationManager = new XamlDesignerSerializationManager(XmlWriter.Create(xamlString, writerSettings))
		{
			XamlWriterMode = XamlWriterMode.Expression
		};

		using (new XamlSerializationHelper().ApplyHelpers())
		{
			XamlWriter.Save(obj, xamlDesignerSerializationManager);
		}

		return xamlString.ToString();
	}

	public static XElement RemoveNamespaces(this XElement xml)
	{
		foreach (var xe in xml.DescendantsAndSelf())
		{
			// Stripping the namespace by setting the name of the element to it's localname only
			xe.Name = xe.Name.LocalName;
			// replacing all attributes with attributes that are not namespaces and their names are set to only the localname
			xe.ReplaceAttributes(from xattrib in xe.Attributes().Where(xa => !xa.IsNamespaceDeclaration) select new XAttribute(xattrib.Name.LocalName, xattrib.Value));
		}

		return xml;
	}

	private class BindingExpressionConvertor : ExpressionConverter
	{
		public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
		{
			//return base.CanConvertTo(context, destinationType);
			return true;
		}

		public override object? ConvertTo(ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value, Type destinationType)
		{
			if (destinationType == typeof(MarkupExtension))
			{
				if (value is not BindingExpression bindingExpression)
				{
					throw new Exception();
				}

				return bindingExpression.ParentBinding;
			}

			return base.ConvertTo(context, culture, value, destinationType);
		}
	}

	private class XamlSerializationHelper : IDisposable
	{
		private readonly Dictionary<TypeDescriptionProvider, Type> registrations = new();

		public XamlSerializationHelper ApplyHelpers()
		{
			this.Register<BindingExpression, BindingExpressionConvertor>();
			return this;
		}

		private void Register<TObject, TConverter>()
		{
			var attr = new Attribute[1];
			var vConv = new TypeConverterAttribute(typeof(TConverter));
			attr[0] = vConv;

			var typeDescriptionProvider = TypeDescriptor.AddAttributes(typeof(TObject), attr);
			this.registrations.Add(typeDescriptionProvider, typeof(TObject));
		}

		public void Dispose()
		{
			foreach (var registration in this.registrations)
			{
				TypeDescriptor.RemoveProvider(registration.Key, registration.Value);
			}
		}
	}
}