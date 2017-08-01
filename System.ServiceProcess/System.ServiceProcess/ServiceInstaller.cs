using System;
using System.Collections;
using System.ComponentModel;
using System.Configuration.Install;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace System.ServiceProcess
{
	public class ServiceInstaller : ComponentInstaller
	{
		private const string NetworkServiceName = "NT AUTHORITY\\NetworkService";

		private const string LocalServiceName = "NT AUTHORITY\\LocalService";

		private EventLogInstaller eventLogInstaller;

		private string serviceName = "";

		private string displayName = "";

		private string description = "";

		private string[] servicesDependedOn = new string[0];

		private ServiceStartMode startType = ServiceStartMode.Manual;

		private bool delayedStartMode;

		private static bool environmentChecked;

		private static bool isWin9x;

		[DefaultValue(""), ServiceProcessDescription("ServiceInstallerDisplayName")]
		public string DisplayName
		{
			get
			{
				return this.displayName;
			}
			set
			{
				if (value == null)
				{
					value = "";
				}
				this.displayName = value;
			}
		}

		[DefaultValue(""), ComVisible(false), ServiceProcessDescription("ServiceInstallerDescription")]
		public string Description
		{
			get
			{
				return this.description;
			}
			set
			{
				if (value == null)
				{
					value = "";
				}
				this.description = value;
			}
		}

		[ServiceProcessDescription("ServiceInstallerServicesDependedOn")]
		public string[] ServicesDependedOn
		{
			get
			{
				return this.servicesDependedOn;
			}
			set
			{
				if (value == null)
				{
					value = new string[0];
				}
				this.servicesDependedOn = value;
			}
		}

		[DefaultValue(""), TypeConverter("System.Diagnostics.Design.StringValueConverter, System.Design, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a"), ServiceProcessDescription("ServiceInstallerServiceName")]
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
					value = "";
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
				this.eventLogInstaller.Source = value;
			}
		}

		[DefaultValue(ServiceStartMode.Manual), ServiceProcessDescription("ServiceInstallerStartType")]
		public ServiceStartMode StartType
		{
			get
			{
				return this.startType;
			}
			set
			{
				if (!Enum.IsDefined(typeof(ServiceStartMode), value))
				{
					throw new InvalidEnumArgumentException("value", (int)value, typeof(ServiceStartMode));
				}
				if (value == ServiceStartMode.Boot || value == ServiceStartMode.System)
				{
					throw new ArgumentException(Res.GetString("ServiceStartType", new object[]
					{
						value
					}));
				}
				this.startType = value;
			}
		}

		[DefaultValue(false), ServiceProcessDescription("ServiceInstallerDelayedAutoStart")]
		public bool DelayedAutoStart
		{
			get
			{
				return this.delayedStartMode;
			}
			set
			{
				this.delayedStartMode = value;
			}
		}

		public ServiceInstaller()
		{
			this.eventLogInstaller = new EventLogInstaller();
			this.eventLogInstaller.Log = "Application";
			this.eventLogInstaller.Source = "";
			this.eventLogInstaller.UninstallAction = UninstallAction.Remove;
			base.Installers.Add(this.eventLogInstaller);
		}

		internal static void CheckEnvironment()
		{
			if (ServiceInstaller.environmentChecked)
			{
				if (ServiceInstaller.isWin9x)
				{
					throw new PlatformNotSupportedException(Res.GetString("CantControlOnWin9x"));
				}
				return;
			}
			else
			{
				ServiceInstaller.isWin9x = (Environment.OSVersion.Platform != PlatformID.Win32NT);
				ServiceInstaller.environmentChecked = true;
				if (ServiceInstaller.isWin9x)
				{
					throw new PlatformNotSupportedException(Res.GetString("CantInstallOnWin9x"));
				}
				return;
			}
		}

		public override void CopyFromComponent(IComponent component)
		{
			if (!(component is ServiceBase))
			{
				throw new ArgumentException(Res.GetString("NotAService"));
			}
			ServiceBase serviceBase = (ServiceBase)component;
			this.ServiceName = serviceBase.ServiceName;
		}

		public override void Install(IDictionary stateSaver)
		{
			base.Context.LogMessage(Res.GetString("InstallingService", new object[]
			{
				this.ServiceName
			}));
			try
			{
				ServiceInstaller.CheckEnvironment();
				string servicesStartName = null;
				string password = null;
				ServiceProcessInstaller serviceProcessInstaller = null;
				if (base.Parent is ServiceProcessInstaller)
				{
					serviceProcessInstaller = (ServiceProcessInstaller)base.Parent;
				}
				else
				{
					for (int i = 0; i < base.Parent.Installers.Count; i++)
					{
						if (base.Parent.Installers[i] is ServiceProcessInstaller)
						{
							serviceProcessInstaller = (ServiceProcessInstaller)base.Parent.Installers[i];
							break;
						}
					}
				}
				if (serviceProcessInstaller == null)
				{
					throw new InvalidOperationException(Res.GetString("NoInstaller"));
				}
				switch (serviceProcessInstaller.Account)
				{
				case ServiceAccount.LocalService:
					servicesStartName = "NT AUTHORITY\\LocalService";
					break;
				case ServiceAccount.NetworkService:
					servicesStartName = "NT AUTHORITY\\NetworkService";
					break;
				case ServiceAccount.User:
					servicesStartName = serviceProcessInstaller.Username;
					password = serviceProcessInstaller.Password;
					break;
				}
				string text = base.Context.Parameters["assemblypath"];
				if (string.IsNullOrEmpty(text))
				{
					throw new InvalidOperationException(Res.GetString("FileName"));
				}
				if (text.IndexOf('"') == -1)
				{
					text = "\"" + text + "\"";
				}
				if (!ServiceInstaller.ValidateServiceName(this.ServiceName))
				{
					throw new InvalidOperationException(Res.GetString("ServiceName", new object[]
					{
						this.ServiceName,
						80.ToString(CultureInfo.CurrentCulture)
					}));
				}
				if (this.DisplayName.Length > 255)
				{
					throw new ArgumentException(Res.GetString("DisplayNameTooLong", new object[]
					{
						this.DisplayName
					}));
				}
				string dependencies = null;
				if (this.ServicesDependedOn.Length != 0)
				{
					StringBuilder stringBuilder = new StringBuilder();
					for (int j = 0; j < this.ServicesDependedOn.Length; j++)
					{
						string text2 = this.ServicesDependedOn[j];
						try
						{
							ServiceController serviceController = new ServiceController(text2, ".");
							text2 = serviceController.ServiceName;
						}
						catch
						{
						}
						stringBuilder.Append(text2);
						stringBuilder.Append('\0');
					}
					stringBuilder.Append('\0');
					dependencies = stringBuilder.ToString();
				}
				IntPtr intPtr = SafeNativeMethods.OpenSCManager(null, null, 983103);
				IntPtr intPtr2 = IntPtr.Zero;
				if (intPtr == IntPtr.Zero)
				{
					throw new InvalidOperationException(Res.GetString("OpenSC", new object[]
					{
						"."
					}), new Win32Exception());
				}
				int serviceType = 16;
				int num = 0;
				for (int k = 0; k < base.Parent.Installers.Count; k++)
				{
					if (base.Parent.Installers[k] is ServiceInstaller)
					{
						num++;
						if (num > 1)
						{
							break;
						}
					}
				}
				if (num > 1)
				{
					serviceType = 32;
				}
				try
				{
					intPtr2 = NativeMethods.CreateService(intPtr, this.ServiceName, this.DisplayName, 983551, serviceType, (int)this.StartType, 1, text, null, IntPtr.Zero, dependencies, servicesStartName, password);
					if (intPtr2 == IntPtr.Zero)
					{
						throw new Win32Exception();
					}
					if (this.Description.Length != 0)
					{
						NativeMethods.SERVICE_DESCRIPTION sERVICE_DESCRIPTION = default(NativeMethods.SERVICE_DESCRIPTION);
						sERVICE_DESCRIPTION.description = Marshal.StringToHGlobalUni(this.Description);
						bool flag = NativeMethods.ChangeServiceConfig2(intPtr2, 1u, ref sERVICE_DESCRIPTION);
						Marshal.FreeHGlobal(sERVICE_DESCRIPTION.description);
						if (!flag)
						{
							throw new Win32Exception();
						}
					}
					if (Environment.OSVersion.Version.Major > 5 && this.StartType == ServiceStartMode.Automatic)
					{
						NativeMethods.SERVICE_DELAYED_AUTOSTART_INFO sERVICE_DELAYED_AUTOSTART_INFO = default(NativeMethods.SERVICE_DELAYED_AUTOSTART_INFO);
						sERVICE_DELAYED_AUTOSTART_INFO.fDelayedAutostart = this.DelayedAutoStart;
						if (!NativeMethods.ChangeServiceConfig2(intPtr2, 3u, ref sERVICE_DELAYED_AUTOSTART_INFO))
						{
							throw new Win32Exception();
						}
					}
					stateSaver["installed"] = true;
				}
				finally
				{
					if (intPtr2 != IntPtr.Zero)
					{
						SafeNativeMethods.CloseServiceHandle(intPtr2);
					}
					SafeNativeMethods.CloseServiceHandle(intPtr);
				}
				base.Context.LogMessage(Res.GetString("InstallOK", new object[]
				{
					this.ServiceName
				}));
			}
			finally
			{
				base.Install(stateSaver);
			}
		}

		public override bool IsEquivalentInstaller(ComponentInstaller otherInstaller)
		{
			ServiceInstaller serviceInstaller = otherInstaller as ServiceInstaller;
			return serviceInstaller != null && serviceInstaller.ServiceName == this.ServiceName;
		}

		private void RemoveService()
		{
			base.Context.LogMessage(Res.GetString("ServiceRemoving", new object[]
			{
				this.ServiceName
			}));
			IntPtr intPtr = SafeNativeMethods.OpenSCManager(null, null, 983103);
			if (intPtr == IntPtr.Zero)
			{
				throw new Win32Exception();
			}
			IntPtr intPtr2 = IntPtr.Zero;
			try
			{
				intPtr2 = NativeMethods.OpenService(intPtr, this.ServiceName, 65536);
				if (intPtr2 == IntPtr.Zero)
				{
					throw new Win32Exception();
				}
				NativeMethods.DeleteService(intPtr2);
			}
			finally
			{
				if (intPtr2 != IntPtr.Zero)
				{
					SafeNativeMethods.CloseServiceHandle(intPtr2);
				}
				SafeNativeMethods.CloseServiceHandle(intPtr);
			}
			base.Context.LogMessage(Res.GetString("ServiceRemoved", new object[]
			{
				this.ServiceName
			}));
			try
			{
				using (ServiceController serviceController = new ServiceController(this.ServiceName))
				{
					if (serviceController.Status != ServiceControllerStatus.Stopped)
					{
						base.Context.LogMessage(Res.GetString("TryToStop", new object[]
						{
							this.ServiceName
						}));
						serviceController.Stop();
						int num = 10;
						serviceController.Refresh();
						while (serviceController.Status != ServiceControllerStatus.Stopped && num > 0)
						{
							Thread.Sleep(1000);
							serviceController.Refresh();
							num--;
						}
					}
				}
			}
			catch
			{
			}
			Thread.Sleep(5000);
		}

		public override void Rollback(IDictionary savedState)
		{
			base.Rollback(savedState);
			object obj = savedState["installed"];
			if (obj == null || !(bool)obj)
			{
				return;
			}
			this.RemoveService();
		}

		private bool ShouldSerializeServicesDependedOn()
		{
			return this.servicesDependedOn != null && this.servicesDependedOn.Length != 0;
		}

		public override void Uninstall(IDictionary savedState)
		{
			base.Uninstall(savedState);
			this.RemoveService();
		}

		private static bool ValidateServiceName(string name)
		{
			if (name == null || name.Length == 0 || name.Length > 80)
			{
				return false;
			}
			char[] array = name.ToCharArray();
			for (int i = 0; i < array.Length; i++)
			{
				if (array[i] < ' ' || array[i] == '/' || array[i] == '\\')
				{
					return false;
				}
			}
			return true;
		}
	}
}
