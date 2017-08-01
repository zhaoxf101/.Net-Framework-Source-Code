using System;
using System.ComponentModel;

namespace System.ServiceProcess
{
	[AttributeUsage(AttributeTargets.All)]
	public class ServiceProcessDescriptionAttribute : DescriptionAttribute
	{
		private bool replaced;

		public override string Description
		{
			get
			{
				if (!this.replaced)
				{
					this.replaced = true;
					base.DescriptionValue = Res.GetString(base.Description);
				}
				return base.Description;
			}
		}

		public ServiceProcessDescriptionAttribute(string description) : base(description)
		{
		}
	}
}
