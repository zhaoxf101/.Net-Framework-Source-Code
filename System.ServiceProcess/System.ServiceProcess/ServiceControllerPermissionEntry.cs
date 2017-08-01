using System;
using System.ComponentModel;
using System.Globalization;
using System.Security.Permissions;

namespace System.ServiceProcess
{
	[Serializable]
	public class ServiceControllerPermissionEntry
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
		}

		public ServiceControllerPermissionAccess PermissionAccess
		{
			get
			{
				return this.permissionAccess;
			}
		}

		public string ServiceName
		{
			get
			{
				return this.serviceName;
			}
		}

		public ServiceControllerPermissionEntry()
		{
			this.machineName = ".";
			this.serviceName = "*";
			this.permissionAccess = ServiceControllerPermissionAccess.Browse;
		}

		public ServiceControllerPermissionEntry(ServiceControllerPermissionAccess permissionAccess, string machineName, string serviceName)
		{
			if (serviceName == null)
			{
				throw new ArgumentNullException("serviceName");
			}
			if (!ServiceController.ValidServiceName(serviceName))
			{
				throw new ArgumentException(Res.GetString("ServiceName", new object[]
				{
					serviceName,
					80.ToString(CultureInfo.CurrentCulture)
				}));
			}
			if (!SyntaxCheck.CheckMachineName(machineName))
			{
				throw new ArgumentException(Res.GetString("BadMachineName", new object[]
				{
					machineName
				}));
			}
			this.permissionAccess = permissionAccess;
			this.machineName = machineName;
			this.serviceName = serviceName;
		}

		internal ServiceControllerPermissionEntry(ResourcePermissionBaseEntry baseEntry)
		{
			this.permissionAccess = (ServiceControllerPermissionAccess)baseEntry.PermissionAccess;
			this.machineName = baseEntry.PermissionAccessPath[0];
			this.serviceName = baseEntry.PermissionAccessPath[1];
		}

		internal ResourcePermissionBaseEntry GetBaseEntry()
		{
			return new ResourcePermissionBaseEntry((int)this.PermissionAccess, new string[]
			{
				this.MachineName,
				this.ServiceName
			});
		}
	}
}
