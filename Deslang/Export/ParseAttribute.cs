using System;
using System.ComponentModel;
using System.ComponentModel.Design.Serialization;
using System.Globalization;

namespace Glory
{
	class ParseAttributeConverter : TypeConverter
	{
		public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
		{
			if (typeof(InstanceDescriptor) == destinationType)
				return true;
			return base.CanConvertTo(context, destinationType);
		}
		public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
		{
			if (typeof(InstanceDescriptor) == destinationType)
			{
				var attr = (ParseAttribute)value;
				return new InstanceDescriptor(typeof(ParseAttribute).GetConstructor(new Type[] { typeof(string), typeof(object) }), new object[] { attr.Name, attr.Value });
			}
			return base.ConvertTo(context, culture, value, destinationType);
		}
	}
	[TypeConverter(typeof(ParseAttributeConverter))]
	public struct ParseAttribute
	{
		string _name;
		object _value;
		public string Name {
			get { return _name; }
		}
		public object Value {
			get { return _value; }
		}
		public ParseAttribute(string name, object value)
		{
			_name = name;
			_value = value;
		}
	}
}
