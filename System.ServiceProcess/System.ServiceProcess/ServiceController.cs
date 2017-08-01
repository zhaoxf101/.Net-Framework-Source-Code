using System;
using System.Collections;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Permissions;
using System.ServiceProcess.Design;
using System.Text;
using System.Threading;

namespace System.ServiceProcess
{
	[Designer("System.ServiceProcess.Design.ServiceControllerDesigner, System.Design, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a"), ServiceProcessDescription("ServiceControllerDesc")]
	public class ServiceController : Component
	{
		private string machineName = ".";

		private string name = "";

		private string displayName = "";

		private string eitherName = "";

		private int commandsAccepted;

		private ServiceControllerStatus status;

		private IntPtr serviceManagerHandle;

		private bool statusGenerated;

		private bool controlGranted;

		private bool browseGranted;

		private ServiceController[] dependentServices;

		private ServiceController[] servicesDependedOn;

		private int type;

		private bool disposed;

		private bool startTypeInitialized;

		private ServiceStartMode startType;

		private const int DISPLAYNAMEBUFFERSIZE = 256;

		private static readonly int UnknownEnvironment = 0;

		private static readonly int NtEnvironment = 1;

		private static readonly int NonNtEnvironment = 2;

		private static int environment = ServiceController.UnknownEnvironment;

		private static object s_InternalSyncObject;

		private static object InternalSyncObject
		{
			get
			{
				if (ServiceController.s_InternalSyncObject == null)
				{
					object value = new object();
					Interlocked.CompareExchange(ref ServiceController.s_InternalSyncObject, value, null);
				}
				return ServiceController.s_InternalSyncObject;
			}
		}

		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden), ServiceProcessDescription("SPCanPauseAndContinue")]
		public bool CanPauseAndContinue
		{
			get
			{
				this.GenerateStatus();
				return (this.commandsAccepted & 2) != 0;
			}
		}

		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden), ServiceProcessDescription("SPCanShutdown")]
		public bool CanShutdown
		{
			get
			{
				this.GenerateStatus();
				return (this.commandsAccepted & 4) != 0;
			}
		}

		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden), ServiceProcessDescription("SPCanStop")]
		public bool CanStop
		{
			get
			{
				this.GenerateStatus();
				return (this.commandsAccepted & 1) != 0;
			}
		}

		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden), ReadOnly(true), ServiceProcessDescription("SPDisplayName")]
		public string DisplayName
		{
			get
			{
				if (this.displayName.Length == 0 && (this.eitherName.Length > 0 || this.name.Length > 0))
				{
					this.GenerateNames();
				}
				return this.displayName;
			}
			set
			{
				if (value == null)
				{
					throw new ArgumentNullException("value");
				}
				if (string.Compare(value, this.displayName, StringComparison.OrdinalIgnoreCase) == 0)
				{
					this.displayName = value;
					return;
				}
				this.Close();
				this.displayName = value;
				this.name = "";
			}
		}

		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden), ServiceProcessDescription("SPDependentServices")]
		public ServiceController[] DependentServices
		{
			get
			{
				if (!this.browseGranted)
				{
					ServiceControllerPermission serviceControllerPermission = new ServiceControllerPermission(ServiceControllerPermissionAccess.Browse, this.machineName, this.ServiceName);
					serviceControllerPermission.Demand();
					this.browseGranted = true;
				}
				if (this.dependentServices == null)
				{
					IntPtr serviceHandle = this.GetServiceHandle(8);
					try
					{
						int num = 0;
						int num2 = 0;
						bool flag = UnsafeNativeMethods.EnumDependentServices(serviceHandle, 3, (IntPtr)0, 0, ref num, ref num2);
						if (flag)
						{
							this.dependentServices = new ServiceController[0];
							return this.dependentServices;
						}
						if (Marshal.GetLastWin32Error() != 234)
						{
							throw ServiceController.CreateSafeWin32Exception();
						}
						IntPtr intPtr = Marshal.AllocHGlobal((IntPtr)num);
						try
						{
							if (!UnsafeNativeMethods.EnumDependentServices(serviceHandle, 3, intPtr, num, ref num, ref num2))
							{
								throw ServiceController.CreateSafeWin32Exception();
							}
							this.dependentServices = new ServiceController[num2];
							for (int i = 0; i < num2; i++)
							{
								NativeMethods.ENUM_SERVICE_STATUS structure = new NativeMethods.ENUM_SERVICE_STATUS();
								IntPtr ptr = (IntPtr)((long)intPtr + (long)(i * Marshal.SizeOf(typeof(NativeMethods.ENUM_SERVICE_STATUS))));
								Marshal.PtrToStructure(ptr, structure);
								this.dependentServices[i] = new ServiceController(this.MachineName, structure);
							}
						}
						finally
						{
							Marshal.FreeHGlobal(intPtr);
						}
					}
					finally
					{
						SafeNativeMethods.CloseServiceHandle(serviceHandle);
					}
				}
				return this.dependentServices;
			}
		}

		[Browsable(false), DefaultValue("."), SettingsBindable(true), ServiceProcessDescription("SPMachineName")]
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
				if (string.Compare(this.machineName, value, StringComparison.OrdinalIgnoreCase) == 0)
				{
					this.machineName = value;
					return;
				}
				this.Close();
				this.machineName = value;
			}
		}

		[DefaultValue(""), ReadOnly(true), SettingsBindable(true), TypeConverter(typeof(ServiceNameConverter)), ServiceProcessDescription("SPServiceName")]
		public string ServiceName
		{
			get
			{
				if (this.name.Length == 0 && (this.eitherName.Length > 0 || this.displayName.Length > 0))
				{
					this.GenerateNames();
				}
				return this.name;
			}
			set
			{
				if (value == null)
				{
					throw new ArgumentNullException("value");
				}
				if (string.Compare(value, this.name, StringComparison.OrdinalIgnoreCase) == 0)
				{
					this.name = value;
					return;
				}
				if (!ServiceController.ValidServiceName(value))
				{
					throw new ArgumentException(Res.GetString("ServiceName", new object[]
					{
						value,
						80.ToString(CultureInfo.CurrentCulture)
					}));
				}
				this.Close();
				this.name = value;
				this.displayName = "";
			}
		}

		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden), ServiceProcessDescription("SPServicesDependedOn")]
		public unsafe ServiceController[] ServicesDependedOn
		{
			get
			{
				if (!this.browseGranted)
				{
					ServiceControllerPermission serviceControllerPermission = new ServiceControllerPermission(ServiceControllerPermissionAccess.Browse, this.machineName, this.ServiceName);
					serviceControllerPermission.Demand();
					this.browseGranted = true;
				}
				if (this.servicesDependedOn != null)
				{
					return this.servicesDependedOn;
				}
				IntPtr serviceHandle = this.GetServiceHandle(1);
				ServiceController[] result;
				try
				{
					int num = 0;
					bool flag = UnsafeNativeMethods.QueryServiceConfig(serviceHandle, (IntPtr)0, 0, out num);
					if (flag)
					{
						this.servicesDependedOn = new ServiceController[0];
						result = this.servicesDependedOn;
					}
					else
					{
						if (Marshal.GetLastWin32Error() != 122)
						{
							throw ServiceController.CreateSafeWin32Exception();
						}
						IntPtr intPtr = Marshal.AllocHGlobal((IntPtr)num);
						try
						{
							if (!UnsafeNativeMethods.QueryServiceConfig(serviceHandle, intPtr, num, out num))
							{
								throw ServiceController.CreateSafeWin32Exception();
							}
							NativeMethods.QUERY_SERVICE_CONFIG qUERY_SERVICE_CONFIG = new NativeMethods.QUERY_SERVICE_CONFIG();
							Marshal.PtrToStructure(intPtr, qUERY_SERVICE_CONFIG);
							char* ptr = qUERY_SERVICE_CONFIG.lpDependencies;
							Hashtable hashtable = new Hashtable();
							if (ptr != null)
							{
								StringBuilder stringBuilder = new StringBuilder();
								while (*ptr != '\0')
								{
									stringBuilder.Append(*ptr);
									ptr++;
									if (*ptr == '\0')
									{
										string text = stringBuilder.ToString();
										stringBuilder = new StringBuilder();
										ptr++;
										if (text.StartsWith("+", StringComparison.Ordinal))
										{
											NativeMethods.ENUM_SERVICE_STATUS_PROCESS[] servicesInGroup = ServiceController.GetServicesInGroup(this.machineName, text.Substring(1));
											NativeMethods.ENUM_SERVICE_STATUS_PROCESS[] array = servicesInGroup;
											for (int i = 0; i < array.Length; i++)
											{
												NativeMethods.ENUM_SERVICE_STATUS_PROCESS eNUM_SERVICE_STATUS_PROCESS = array[i];
												if (!hashtable.Contains(eNUM_SERVICE_STATUS_PROCESS.serviceName))
												{
													hashtable.Add(eNUM_SERVICE_STATUS_PROCESS.serviceName, new ServiceController(this.MachineName, eNUM_SERVICE_STATUS_PROCESS));
												}
											}
										}
										else if (!hashtable.Contains(text))
										{
											hashtable.Add(text, new ServiceController(text, this.MachineName));
										}
									}
								}
							}
							this.servicesDependedOn = new ServiceController[hashtable.Count];
							hashtable.Values.CopyTo(this.servicesDependedOn, 0);
							result = this.servicesDependedOn;
						}
						finally
						{
							Marshal.FreeHGlobal(intPtr);
						}
					}
				}
				finally
				{
					SafeNativeMethods.CloseServiceHandle(serviceHandle);
				}
				return result;
			}
		}

		[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public SafeHandle ServiceHandle
		{
			get
			{
				new SecurityPermission(SecurityPermissionFlag.UnmanagedCode).Demand();
				return new SafeServiceHandle(this.GetServiceHandle(983551), true);
			}
		}

		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden), ServiceProcessDescription("SPStatus")]
		public ServiceControllerStatus Status
		{
			get
			{
				this.GenerateStatus();
				return this.status;
			}
		}

		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden), ServiceProcessDescription("SPServiceType")]
		public ServiceType ServiceType
		{
			get
			{
				this.GenerateStatus();
				return (ServiceType)this.type;
			}
		}

		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden), ServiceProcessDescription("SPStartType")]
		public ServiceStartMode StartType
		{
			get
			{
				if (!this.browseGranted)
				{
					ServiceControllerPermission serviceControllerPermission = new ServiceControllerPermission(ServiceControllerPermissionAccess.Browse, this.machineName, this.ServiceName);
					serviceControllerPermission.Demand();
					this.browseGranted = true;
				}
				if (this.startTypeInitialized)
				{
					return this.startType;
				}
				IntPtr intPtr = IntPtr.Zero;
				try
				{
					intPtr = this.GetServiceHandle(1);
					int num = 0;
					bool flag = UnsafeNativeMethods.QueryServiceConfig(intPtr, (IntPtr)0, 0, out num);
					if (Marshal.GetLastWin32Error() != 122)
					{
						throw ServiceController.CreateSafeWin32Exception();
					}
					IntPtr intPtr2 = IntPtr.Zero;
					try
					{
						intPtr2 = Marshal.AllocHGlobal((IntPtr)num);
						if (!UnsafeNativeMethods.QueryServiceConfig(intPtr, intPtr2, num, out num))
						{
							throw ServiceController.CreateSafeWin32Exception();
						}
						NativeMethods.QUERY_SERVICE_CONFIG qUERY_SERVICE_CONFIG = new NativeMethods.QUERY_SERVICE_CONFIG();
						Marshal.PtrToStructure(intPtr2, qUERY_SERVICE_CONFIG);
						this.startType = (ServiceStartMode)qUERY_SERVICE_CONFIG.dwStartType;
						this.startTypeInitialized = true;
					}
					finally
					{
						if (intPtr2 != IntPtr.Zero)
						{
							Marshal.FreeHGlobal(intPtr2);
						}
					}
				}
				finally
				{
					if (intPtr != IntPtr.Zero)
					{
						SafeNativeMethods.CloseServiceHandle(intPtr);
					}
				}
				return this.startType;
			}
		}

		public ServiceController()
		{
			this.type = 319;
		}

		public ServiceController(string name) : this(name, ".")
		{
		}

		public ServiceController(string name, string machineName)
		{
			if (!SyntaxCheck.CheckMachineName(machineName))
			{
				throw new ArgumentException(Res.GetString("BadMachineName", new object[]
				{
					machineName
				}));
			}
			if (name == null || name.Length == 0)
			{
				throw new ArgumentException(Res.GetString("InvalidParameter", new object[]
				{
					"name",
					name
				}));
			}
			this.machineName = machineName;
			this.eitherName = name;
			this.type = 319;
		}

		internal ServiceController(string machineName, NativeMethods.ENUM_SERVICE_STATUS status)
		{
			if (!SyntaxCheck.CheckMachineName(machineName))
			{
				throw new ArgumentException(Res.GetString("BadMachineName", new object[]
				{
					machineName
				}));
			}
			this.machineName = machineName;
			this.name = status.serviceName;
			this.displayName = status.displayName;
			this.commandsAccepted = status.controlsAccepted;
			this.status = (ServiceControllerStatus)status.currentState;
			this.type = status.serviceType;
			this.statusGenerated = true;
		}

		internal ServiceController(string machineName, NativeMethods.ENUM_SERVICE_STATUS_PROCESS status)
		{
			if (!SyntaxCheck.CheckMachineName(machineName))
			{
				throw new ArgumentException(Res.GetString("BadMachineName", new object[]
				{
					machineName
				}));
			}
			this.machineName = machineName;
			this.name = status.serviceName;
			this.displayName = status.displayName;
			this.commandsAccepted = status.controlsAccepted;
			this.status = (ServiceControllerStatus)status.currentState;
			this.type = status.serviceType;
			this.statusGenerated = true;
		}

		private static void CheckEnvironment()
		{
			if (ServiceController.environment == ServiceController.UnknownEnvironment)
			{
				object internalSyncObject = ServiceController.InternalSyncObject;
				lock (internalSyncObject)
				{
					if (ServiceController.environment == ServiceController.UnknownEnvironment)
					{
						if (Environment.OSVersion.Platform == PlatformID.Win32NT)
						{
							ServiceController.environment = ServiceController.NtEnvironment;
						}
						else
						{
							ServiceController.environment = ServiceController.NonNtEnvironment;
						}
					}
				}
			}
			if (ServiceController.environment == ServiceController.NonNtEnvironment)
			{
				throw new PlatformNotSupportedException(Res.GetString("CantControlOnWin9x"));
			}
		}

		public void Close()
		{
			if (this.serviceManagerHandle != (IntPtr)0)
			{
				SafeNativeMethods.CloseServiceHandle(this.serviceManagerHandle);
			}
			this.serviceManagerHandle = (IntPtr)0;
			this.statusGenerated = false;
			this.startTypeInitialized = false;
			this.type = 319;
			this.browseGranted = false;
			this.controlGranted = false;
		}

		private static Win32Exception CreateSafeWin32Exception()
		{
			Win32Exception result = null;
			SecurityPermission securityPermission = new SecurityPermission(PermissionState.Unrestricted);
			securityPermission.Assert();
			try
			{
				result = new Win32Exception();
			}
			finally
			{
				CodeAccessPermission.RevertAssert();
			}
			return result;
		}

		protected override void Dispose(bool disposing)
		{
			this.Close();
			this.disposed = true;
			base.Dispose(disposing);
		}

		private unsafe void GenerateStatus()
		{
			if (!this.statusGenerated)
			{
				if (!this.browseGranted)
				{
					ServiceControllerPermission serviceControllerPermission = new ServiceControllerPermission(ServiceControllerPermissionAccess.Browse, this.machineName, this.ServiceName);
					serviceControllerPermission.Demand();
					this.browseGranted = true;
				}
				IntPtr serviceHandle = this.GetServiceHandle(4);
				try
				{
					NativeMethods.SERVICE_STATUS sERVICE_STATUS = default(NativeMethods.SERVICE_STATUS);
					if (!UnsafeNativeMethods.QueryServiceStatus(serviceHandle, &sERVICE_STATUS))
					{
						throw ServiceController.CreateSafeWin32Exception();
					}
					this.commandsAccepted = sERVICE_STATUS.controlsAccepted;
					this.status = (ServiceControllerStatus)sERVICE_STATUS.currentState;
					this.type = sERVICE_STATUS.serviceType;
					this.statusGenerated = true;
				}
				finally
				{
					SafeNativeMethods.CloseServiceHandle(serviceHandle);
				}
			}
		}

		private void GenerateNames()
		{
			if (this.machineName.Length == 0)
			{
				throw new ArgumentException(Res.GetString("NoMachineName"));
			}
			this.GetDataBaseHandleWithConnectAccess();
			if (this.name.Length == 0)
			{
				string text = this.eitherName;
				if (text.Length == 0)
				{
					text = this.displayName;
				}
				if (text.Length == 0)
				{
					throw new InvalidOperationException(Res.GetString("NoGivenName"));
				}
				int num = 256;
				StringBuilder stringBuilder = new StringBuilder(num);
				bool flag = SafeNativeMethods.GetServiceKeyName(this.serviceManagerHandle, text, stringBuilder, ref num);
				if (flag)
				{
					this.name = stringBuilder.ToString();
					this.displayName = text;
					this.eitherName = "";
				}
				else
				{
					flag = SafeNativeMethods.GetServiceDisplayName(this.serviceManagerHandle, text, stringBuilder, ref num);
					if (!flag && num >= 256)
					{
						stringBuilder = new StringBuilder(++num);
						flag = SafeNativeMethods.GetServiceDisplayName(this.serviceManagerHandle, text, stringBuilder, ref num);
					}
					if (!flag)
					{
						Exception innerException = ServiceController.CreateSafeWin32Exception();
						throw new InvalidOperationException(Res.GetString("NoService", new object[]
						{
							text,
							this.machineName
						}), innerException);
					}
					this.name = text;
					this.displayName = stringBuilder.ToString();
					this.eitherName = "";
				}
			}
			if (this.displayName.Length == 0)
			{
				int num2 = 256;
				StringBuilder stringBuilder2 = new StringBuilder(num2);
				bool serviceDisplayName = SafeNativeMethods.GetServiceDisplayName(this.serviceManagerHandle, this.name, stringBuilder2, ref num2);
				if (!serviceDisplayName && num2 >= 256)
				{
					stringBuilder2 = new StringBuilder(++num2);
					serviceDisplayName = SafeNativeMethods.GetServiceDisplayName(this.serviceManagerHandle, this.name, stringBuilder2, ref num2);
				}
				if (!serviceDisplayName)
				{
					Exception innerException2 = ServiceController.CreateSafeWin32Exception();
					throw new InvalidOperationException(Res.GetString("NoDisplayName", new object[]
					{
						this.name,
						this.machineName
					}), innerException2);
				}
				this.displayName = stringBuilder2.ToString();
			}
		}

		private static IntPtr GetDataBaseHandleWithAccess(string machineName, int serviceControlManaqerAccess)
		{
			ServiceController.CheckEnvironment();
			IntPtr intPtr = IntPtr.Zero;
			if (machineName.Equals(".") || machineName.Length == 0)
			{
				intPtr = SafeNativeMethods.OpenSCManager(null, null, serviceControlManaqerAccess);
			}
			else
			{
				intPtr = SafeNativeMethods.OpenSCManager(machineName, null, serviceControlManaqerAccess);
			}
			if (intPtr == (IntPtr)0)
			{
				Exception innerException = ServiceController.CreateSafeWin32Exception();
				throw new InvalidOperationException(Res.GetString("OpenSC", new object[]
				{
					machineName
				}), innerException);
			}
			return intPtr;
		}

		private void GetDataBaseHandleWithConnectAccess()
		{
			if (this.disposed)
			{
				throw new ObjectDisposedException(base.GetType().Name);
			}
			if (this.serviceManagerHandle == (IntPtr)0)
			{
				this.serviceManagerHandle = ServiceController.GetDataBaseHandleWithAccess(this.MachineName, 1);
			}
		}

		private static IntPtr GetDataBaseHandleWithEnumerateAccess(string machineName)
		{
			return ServiceController.GetDataBaseHandleWithAccess(machineName, 4);
		}

		public static ServiceController[] GetDevices()
		{
			return ServiceController.GetDevices(".");
		}

		public static ServiceController[] GetDevices(string machineName)
		{
			return ServiceController.GetServicesOfType(machineName, 11);
		}

		private IntPtr GetServiceHandle(int desiredAccess)
		{
			this.GetDataBaseHandleWithConnectAccess();
			IntPtr intPtr = UnsafeNativeMethods.OpenService(this.serviceManagerHandle, this.ServiceName, desiredAccess);
			if (intPtr == (IntPtr)0)
			{
				Exception innerException = ServiceController.CreateSafeWin32Exception();
				throw new InvalidOperationException(Res.GetString("OpenService", new object[]
				{
					this.ServiceName,
					this.MachineName
				}), innerException);
			}
			return intPtr;
		}

		public static ServiceController[] GetServices()
		{
			return ServiceController.GetServices(".");
		}

		public static ServiceController[] GetServices(string machineName)
		{
			return ServiceController.GetServicesOfType(machineName, 48);
		}

		private static NativeMethods.ENUM_SERVICE_STATUS_PROCESS[] GetServicesInGroup(string machineName, string group)
		{
			IntPtr intPtr = (IntPtr)0;
			IntPtr intPtr2 = (IntPtr)0;
			int num = 0;
			NativeMethods.ENUM_SERVICE_STATUS_PROCESS[] array;
			try
			{
				intPtr = ServiceController.GetDataBaseHandleWithEnumerateAccess(machineName);
				int num2;
				int num3;
				UnsafeNativeMethods.EnumServicesStatusEx(intPtr, 0, 48, 3, (IntPtr)0, 0, out num2, out num3, ref num, group);
				intPtr2 = Marshal.AllocHGlobal((IntPtr)num2);
				UnsafeNativeMethods.EnumServicesStatusEx(intPtr, 0, 48, 3, intPtr2, num2, out num2, out num3, ref num, group);
				int num4 = num3;
				array = new NativeMethods.ENUM_SERVICE_STATUS_PROCESS[num4];
				for (int i = 0; i < num4; i++)
				{
					IntPtr ptr = (IntPtr)((long)intPtr2 + (long)(i * Marshal.SizeOf(typeof(NativeMethods.ENUM_SERVICE_STATUS_PROCESS))));
					NativeMethods.ENUM_SERVICE_STATUS_PROCESS eNUM_SERVICE_STATUS_PROCESS = new NativeMethods.ENUM_SERVICE_STATUS_PROCESS();
					Marshal.PtrToStructure(ptr, eNUM_SERVICE_STATUS_PROCESS);
					array[i] = eNUM_SERVICE_STATUS_PROCESS;
				}
			}
			finally
			{
				Marshal.FreeHGlobal(intPtr2);
				if (intPtr != (IntPtr)0)
				{
					SafeNativeMethods.CloseServiceHandle(intPtr);
				}
			}
			return array;
		}

		private static ServiceController[] GetServicesOfType(string machineName, int serviceType)
		{
			if (!SyntaxCheck.CheckMachineName(machineName))
			{
				throw new ArgumentException(Res.GetString("BadMachineName", new object[]
				{
					machineName
				}));
			}
			ServiceControllerPermission serviceControllerPermission = new ServiceControllerPermission(ServiceControllerPermissionAccess.Browse, machineName, "*");
			serviceControllerPermission.Demand();
			ServiceController.CheckEnvironment();
			IntPtr intPtr = (IntPtr)0;
			IntPtr intPtr2 = (IntPtr)0;
			int num = 0;
			ServiceController[] array;
			try
			{
				intPtr = ServiceController.GetDataBaseHandleWithEnumerateAccess(machineName);
				int num2;
				int num3;
				UnsafeNativeMethods.EnumServicesStatus(intPtr, serviceType, 3, (IntPtr)0, 0, out num2, out num3, ref num);
				intPtr2 = Marshal.AllocHGlobal((IntPtr)num2);
				UnsafeNativeMethods.EnumServicesStatus(intPtr, serviceType, 3, intPtr2, num2, out num2, out num3, ref num);
				int num4 = num3;
				array = new ServiceController[num4];
				for (int i = 0; i < num4; i++)
				{
					IntPtr ptr = (IntPtr)((long)intPtr2 + (long)(i * Marshal.SizeOf(typeof(NativeMethods.ENUM_SERVICE_STATUS))));
					NativeMethods.ENUM_SERVICE_STATUS structure = new NativeMethods.ENUM_SERVICE_STATUS();
					Marshal.PtrToStructure(ptr, structure);
					array[i] = new ServiceController(machineName, structure);
				}
			}
			finally
			{
				Marshal.FreeHGlobal(intPtr2);
				if (intPtr != (IntPtr)0)
				{
					SafeNativeMethods.CloseServiceHandle(intPtr);
				}
			}
			return array;
		}

		public unsafe void Pause()
		{
			if (!this.controlGranted)
			{
				ServiceControllerPermission serviceControllerPermission = new ServiceControllerPermission(ServiceControllerPermissionAccess.Control, this.machineName, this.ServiceName);
				serviceControllerPermission.Demand();
				this.controlGranted = true;
			}
			IntPtr serviceHandle = this.GetServiceHandle(64);
			try
			{
				NativeMethods.SERVICE_STATUS sERVICE_STATUS = default(NativeMethods.SERVICE_STATUS);
				if (!UnsafeNativeMethods.ControlService(serviceHandle, 2, &sERVICE_STATUS))
				{
					Exception innerException = ServiceController.CreateSafeWin32Exception();
					throw new InvalidOperationException(Res.GetString("PauseService", new object[]
					{
						this.ServiceName,
						this.MachineName
					}), innerException);
				}
			}
			finally
			{
				SafeNativeMethods.CloseServiceHandle(serviceHandle);
			}
		}

		public unsafe void Continue()
		{
			if (!this.controlGranted)
			{
				ServiceControllerPermission serviceControllerPermission = new ServiceControllerPermission(ServiceControllerPermissionAccess.Control, this.machineName, this.ServiceName);
				serviceControllerPermission.Demand();
				this.controlGranted = true;
			}
			IntPtr serviceHandle = this.GetServiceHandle(64);
			try
			{
				NativeMethods.SERVICE_STATUS sERVICE_STATUS = default(NativeMethods.SERVICE_STATUS);
				if (!UnsafeNativeMethods.ControlService(serviceHandle, 3, &sERVICE_STATUS))
				{
					Exception innerException = ServiceController.CreateSafeWin32Exception();
					throw new InvalidOperationException(Res.GetString("ResumeService", new object[]
					{
						this.ServiceName,
						this.MachineName
					}), innerException);
				}
			}
			finally
			{
				SafeNativeMethods.CloseServiceHandle(serviceHandle);
			}
		}

		public unsafe void ExecuteCommand(int command)
		{
			if (!this.controlGranted)
			{
				ServiceControllerPermission serviceControllerPermission = new ServiceControllerPermission(ServiceControllerPermissionAccess.Control, this.machineName, this.ServiceName);
				serviceControllerPermission.Demand();
				this.controlGranted = true;
			}
			IntPtr serviceHandle = this.GetServiceHandle(256);
			try
			{
				NativeMethods.SERVICE_STATUS sERVICE_STATUS = default(NativeMethods.SERVICE_STATUS);
				if (!UnsafeNativeMethods.ControlService(serviceHandle, command, &sERVICE_STATUS))
				{
					Exception innerException = ServiceController.CreateSafeWin32Exception();
					throw new InvalidOperationException(Res.GetString("ControlService", new object[]
					{
						this.ServiceName,
						this.MachineName
					}), innerException);
				}
			}
			finally
			{
				SafeNativeMethods.CloseServiceHandle(serviceHandle);
			}
		}

		public void Refresh()
		{
			this.statusGenerated = false;
			this.startTypeInitialized = false;
			this.dependentServices = null;
			this.servicesDependedOn = null;
		}

		public void Start()
		{
			this.Start(new string[0]);
		}

		public void Start(string[] args)
		{
			if (args == null)
			{
				throw new ArgumentNullException("args");
			}
			if (!this.controlGranted)
			{
				ServiceControllerPermission serviceControllerPermission = new ServiceControllerPermission(ServiceControllerPermissionAccess.Control, this.machineName, this.ServiceName);
				serviceControllerPermission.Demand();
				this.controlGranted = true;
			}
			IntPtr serviceHandle = this.GetServiceHandle(16);
			try
			{
				IntPtr[] array = new IntPtr[args.Length];
				int i = 0;
				try
				{
					for (i = 0; i < args.Length; i++)
					{
						if (args[i] == null)
						{
							throw new ArgumentNullException(Res.GetString("ArgsCantBeNull"), "args");
						}
						array[i] = Marshal.StringToHGlobalUni(args[i]);
					}
				}
				catch
				{
					for (int j = 0; j < i; j++)
					{
						Marshal.FreeHGlobal(array[i]);
					}
					throw;
				}
				GCHandle gCHandle = default(GCHandle);
				try
				{
					gCHandle = GCHandle.Alloc(array, GCHandleType.Pinned);
					if (!UnsafeNativeMethods.StartService(serviceHandle, args.Length, gCHandle.AddrOfPinnedObject()))
					{
						Exception innerException = ServiceController.CreateSafeWin32Exception();
						throw new InvalidOperationException(Res.GetString("CannotStart", new object[]
						{
							this.ServiceName,
							this.MachineName
						}), innerException);
					}
				}
				finally
				{
					for (i = 0; i < args.Length; i++)
					{
						Marshal.FreeHGlobal(array[i]);
					}
					if (gCHandle.IsAllocated)
					{
						gCHandle.Free();
					}
				}
			}
			finally
			{
				SafeNativeMethods.CloseServiceHandle(serviceHandle);
			}
		}

		public unsafe void Stop()
		{
			if (!this.controlGranted)
			{
				ServiceControllerPermission serviceControllerPermission = new ServiceControllerPermission(ServiceControllerPermissionAccess.Control, this.machineName, this.ServiceName);
				serviceControllerPermission.Demand();
				this.controlGranted = true;
			}
			IntPtr serviceHandle = this.GetServiceHandle(32);
			try
			{
				for (int i = 0; i < this.DependentServices.Length; i++)
				{
					ServiceController serviceController = this.DependentServices[i];
					serviceController.Refresh();
					if (serviceController.Status != ServiceControllerStatus.Stopped)
					{
						serviceController.Stop();
						serviceController.WaitForStatus(ServiceControllerStatus.Stopped, new TimeSpan(0, 0, 30));
					}
				}
				NativeMethods.SERVICE_STATUS sERVICE_STATUS = default(NativeMethods.SERVICE_STATUS);
				if (!UnsafeNativeMethods.ControlService(serviceHandle, 1, &sERVICE_STATUS))
				{
					Exception innerException = ServiceController.CreateSafeWin32Exception();
					throw new InvalidOperationException(Res.GetString("StopService", new object[]
					{
						this.ServiceName,
						this.MachineName
					}), innerException);
				}
			}
			finally
			{
				SafeNativeMethods.CloseServiceHandle(serviceHandle);
			}
		}

		internal static bool ValidServiceName(string serviceName)
		{
			if (serviceName == null)
			{
				return false;
			}
			if (serviceName.Length > 80 || serviceName.Length == 0)
			{
				return false;
			}
			char[] array = serviceName.ToCharArray();
			for (int i = 0; i < array.Length; i++)
			{
				char c = array[i];
				if (c == '\\' || c == '/')
				{
					return false;
				}
			}
			return true;
		}

		public void WaitForStatus(ServiceControllerStatus desiredStatus)
		{
			this.WaitForStatus(desiredStatus, TimeSpan.MaxValue);
		}

		public void WaitForStatus(ServiceControllerStatus desiredStatus, TimeSpan timeout)
		{
			if (!Enum.IsDefined(typeof(ServiceControllerStatus), desiredStatus))
			{
				throw new InvalidEnumArgumentException("desiredStatus", (int)desiredStatus, typeof(ServiceControllerStatus));
			}
			DateTime utcNow = DateTime.UtcNow;
			this.Refresh();
			while (this.Status != desiredStatus)
			{
				if (DateTime.UtcNow - utcNow > timeout)
				{
					throw new TimeoutException(Res.GetString("Timeout"));
				}
				Thread.Sleep(250);
				this.Refresh();
			}
		}
	}
}
