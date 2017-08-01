using System;
using System.ComponentModel;
using System.Globalization;

namespace System.ServiceProcess.Design
{
	internal class ServiceNameConverter : TypeConverter
	{
		private TypeConverter.StandardValuesCollection values;

		private string previousMachineName;

		public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
		{
			return sourceType == typeof(string) || base.CanConvertFrom(context, sourceType);
		}

		public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
		{
			if (value is string)
			{
				return ((string)value).Trim();
			}
			return base.ConvertFrom(context, culture, value);
		}

		public override TypeConverter.StandardValuesCollection GetStandardValues(ITypeDescriptorContext context)
		{
			ServiceController serviceController = (context == null) ? null : (context.Instance as ServiceController);
			string text = ".";
			if (serviceController != null)
			{
				text = serviceController.MachineName;
			}
			if (this.values == null || text != this.previousMachineName)
			{
				try
				{
					ServiceController[] services = ServiceController.GetServices(text);
					string[] array = new string[services.Length];
					for (int i = 0; i < services.Length; i++)
					{
						array[i] = services[i].ServiceName;
					}
					this.values = new TypeConverter.StandardValuesCollection(array);
					this.previousMachineName = text;
				}
				catch
				{
				}
			}
			return this.values;
		}

		public override bool GetStandardValuesExclusive(ITypeDescriptorContext context)
		{
			return false;
		}

		public override bool GetStandardValuesSupported(ITypeDescriptorContext context)
		{
			return true;
		}
	}
}
