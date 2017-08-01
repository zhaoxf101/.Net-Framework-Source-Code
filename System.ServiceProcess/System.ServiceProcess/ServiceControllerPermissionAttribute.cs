using System;
using System.ComponentModel;
using System.Globalization;
using System.Security;
using System.Security.Permissions;

namespace System.ServiceProcess
{
	[AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Constructor | AttributeTargets.Method | AttributeTargets.Event, AllowMultiple = true, Inherited = false)]
	[Serializable]
	public class ServiceControllerPermissionAttribute : CodeAccessSecurityAttribute
	{
		private string machineName;

		private string serviceName;

		private ServiceControllerPermissionAccess permissionAccess;

		public string MachineName
		{
			get
			{
				return this.machineName;
			}
			set
			{
				if (!SyntaxCheck.CheckMachineName(value))
				{
					throw new ArgumentException(Res.GetString("BadMachineName", new object[]
					{
						value
					}));
				}
				this.machineName = value;
			}
		}

		public ServiceControllerPermissionAccess PermissionAccess
		{
			get
			{
				return this.permissionAccess;
			}
			set
			{
				this.permissionAccess = value;
			}
		}

		public string ServiceName
		{
			get
			{
				return this.serviceName;
			}
			set
			{
				if (value == null)
				{
					throw new ArgumentNullException("value");
				}
				if (!ServiceController.ValidServiceName(value))
				{
					throw new ArgumentException(Res.GetString("ServiceName", new object[]
					{
						value,
						80.ToString(CultureInfo.CurrentCulture)
					}));
				}
				this.serviceName = value;
			}
		}

		public ServiceControllerPermissionAttribute(SecurityAction action) : base(action)
		{
			this.machineName = ".";
			this.serviceName = "*";
			this.permissionAccess = ServiceControllerPermissionAccess.Browse;
		}

		public override IPermission CreatePermission()
		{
			if (base.Unrestricted)
			{
				return new ServiceControllerPermission(PermissionState.Unrestricted);
			}
			return new ServiceControllerPermission(this.PermissionAccess, this.MachineName, this.ServiceName);
		}
	}
}
