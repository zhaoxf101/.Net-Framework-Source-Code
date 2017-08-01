using System;
using System.ComponentModel;

namespace System.Configuration.Install
{
	internal class InstallerParentConverter : ReferenceConverter
	{
		public InstallerParentConverter(Type type) : base(type)
		{
		}

		public override TypeConverter.StandardValuesCollection GetStandardValues(ITypeDescriptorContext context)
		{
			TypeConverter.StandardValuesCollection standardValues = base.GetStandardValues(context);
			object instance = context.Instance;
			int i = 0;
			int num = 0;
			object[] array = new object[standardValues.Count - 1];
			while (i < standardValues.Count)
			{
				if (standardValues[i] != instance)
				{
					array[num] = standardValues[i];
					num++;
				}
				i++;
			}
			return new TypeConverter.StandardValuesCollection(array);
		}
	}
}
