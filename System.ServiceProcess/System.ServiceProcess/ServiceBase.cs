using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Permissions;
using System.Threading;

namespace System.ServiceProcess
{
	[InstallerType(typeof(ServiceProcessInstaller))]
	public class ServiceBase : Component
	{
		private delegate void DeferredHandlerDelegate();

		private delegate void DeferredHandlerDelegateCommand(int command);

		private delegate void DeferredHandlerDelegateAdvanced(int eventType, IntPtr eventData);

		private delegate void DeferredHandlerDelegateAdvancedSession(int eventType, int sessionId);

		private NativeMethods.SERVICE_STATUS status;

		private IntPtr statusHandle;

		private NativeMethods.ServiceControlCallback commandCallback;

		private NativeMethods.ServiceControlCallbackEx commandCallbackEx;

		private NativeMethods.ServiceMainCallback mainCallback;

		private IntPtr handleName;

		private ManualResetEvent startCompletedSignal;

		private int acceptedCommands;

		private bool autoLog;

		private string serviceName;

		private EventLog eventLog;

		private bool nameFrozen;

		private bool commandPropsFrozen;

		private bool disposed;

		private bool initialized;

		private bool isServiceHosted;

		public const int MaxNameLength = 80;

		[DefaultValue(true), ServiceProcessDescription("SBAutoLog")]
		public bool AutoLog
		{
			get
			{
				return this.autoLog;
			}
			set
			{
				this.autoLog = value;
			}
		}

		[ComVisible(false)]
		public int ExitCode
		{
			get
			{
				return this.status.win32ExitCode;
			}
			set
			{
				this.status.win32ExitCode = value;
			}
		}

		[DefaultValue(false)]
		public bool CanHandlePowerEvent
		{
			get
			{
				return (this.acceptedCommands & 64) != 0;
			}
			set
			{
				if (this.commandPropsFrozen)
				{
					throw new InvalidOperationException(Res.GetString("CannotChangeProperties"));
				}
				if (value)
				{
					this.acceptedCommands |= 64;
					return;
				}
				this.acceptedCommands &= -65;
			}
		}

		[DefaultValue(false), ComVisible(false)]
		public bool CanHandleSessionChangeEvent
		{
			get
			{
				return (this.acceptedCommands & 128) != 0;
			}
			set
			{
				if (this.commandPropsFrozen)
				{
					throw new InvalidOperationException(Res.GetString("CannotChangeProperties"));
				}
				if (value)
				{
					this.acceptedCommands |= 128;
					return;
				}
				this.acceptedCommands &= -129;
			}
		}

		[DefaultValue(false)]
		public bool CanPauseAndContinue
		{
			get
			{
				return (this.acceptedCommands & 2) != 0;
			}
			set
			{
				if (this.commandPropsFrozen)
				{
					throw new InvalidOperationException(Res.GetString("CannotChangeProperties"));
				}
				if (value)
				{
					this.acceptedCommands |= 2;
					return;
				}
				this.acceptedCommands &= -3;
			}
		}

		[DefaultValue(false)]
		public bool CanShutdown
		{
			get
			{
				return (this.acceptedCommands & 4) != 0;
			}
			set
			{
				if (this.commandPropsFrozen)
				{
					throw new InvalidOperationException(Res.GetString("CannotChangeProperties"));
				}
				if (value)
				{
					this.acceptedCommands |= 4;
					return;
				}
				this.acceptedCommands &= -5;
			}
		}

		[DefaultValue(true)]
		public bool CanStop
		{
			get
			{
				return (this.acceptedCommands & 1) != 0;
			}
			set
			{
				if (this.commandPropsFrozen)
				{
					throw new InvalidOperationException(Res.GetString("CannotChangeProperties"));
				}
				if (value)
				{
					this.acceptedCommands |= 1;
					return;
				}
				this.acceptedCommands &= -2;
			}
		}

		[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public virtual EventLog EventLog
		{
			get
			{
				if (this.eventLog == null)
				{
					this.eventLog = new EventLog();
					this.eventLog.Source = this.ServiceName;
					this.eventLog.Log = "Application";
				}
				return this.eventLog;
			}
		}

		[EditorBrowsable(EditorBrowsableState.Advanced)]
		protected IntPtr ServiceHandle
		{
			get
			{
				new SecurityPermission(SecurityPermissionFlag.UnmanagedCode).Demand();
				return this.statusHandle;
			}
		}

		[TypeConverter("System.Diagnostics.Design.StringValueConverter, System.Design, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a"), ServiceProcessDescription("SBServiceName")]
		public string ServiceName
		{
			get
			{
				return this.serviceName;
			}
			set
			{
				if (this.nameFrozen)
				{
					throw new InvalidOperationException(Res.GetString("CannotChangeName"));
				}
				if (value != "" && !ServiceController.ValidServiceName(value))
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

		private static bool IsRTLResources
		{
			get
			{
				return Res.GetString("RTL") != "RTL_False";
			}
		}

		public ServiceBase()
		{
			this.acceptedCommands = 1;
			this.AutoLog = true;
			this.ServiceName = "";
		}

		[ComVisible(false)]
		public unsafe void RequestAdditionalTime(int milliseconds)
		{
			fixed (NativeMethods.SERVICE_STATUS* ptr = &this.status)
			{
				if (this.status.currentState != 5 && this.status.currentState != 2 && this.status.currentState != 3 && this.status.currentState != 6)
				{
					throw new InvalidOperationException(Res.GetString("NotInPendingState"));
				}
				this.status.waitHint = milliseconds;
				this.status.checkPoint = this.status.checkPoint + 1;
				NativeMethods.SetServiceStatus(this.statusHandle, ptr);
			}
		}

		protected override void Dispose(bool disposing)
		{
			if (this.handleName != (IntPtr)0)
			{
				Marshal.FreeHGlobal(this.handleName);
				this.handleName = (IntPtr)0;
			}
			this.nameFrozen = false;
			this.commandPropsFrozen = false;
			this.disposed = true;
			base.Dispose(disposing);
		}

		protected virtual void OnContinue()
		{
		}

		protected virtual void OnPause()
		{
		}

		protected virtual bool OnPowerEvent(PowerBroadcastStatus powerStatus)
		{
			return true;
		}

		protected virtual void OnSessionChange(SessionChangeDescription changeDescription)
		{
		}

		protected virtual void OnShutdown()
		{
		}

		protected virtual void OnStart(string[] args)
		{
		}

		protected virtual void OnStop()
		{
		}

		private unsafe void DeferredContinue()
		{
			fixed (NativeMethods.SERVICE_STATUS* ptr = &this.status)
			{
				try
				{
					this.OnContinue();
					this.WriteEventLogEntry(Res.GetString("ContinueSuccessful"));
					this.status.currentState = 4;
				}
				catch (Exception ex)
				{
					this.status.currentState = 7;
					this.WriteEventLogEntry(Res.GetString("ContinueFailed", new object[]
					{
						ex.ToString()
					}), EventLogEntryType.Error);
					throw;
				}
				finally
				{
					NativeMethods.SetServiceStatus(this.statusHandle, ptr);
				}
			}
		}

		private void DeferredCustomCommand(int command)
		{
			try
			{
				this.OnCustomCommand(command);
				this.WriteEventLogEntry(Res.GetString("CommandSuccessful"));
			}
			catch (Exception ex)
			{
				this.WriteEventLogEntry(Res.GetString("CommandFailed", new object[]
				{
					ex.ToString()
				}), EventLogEntryType.Error);
				throw;
			}
		}

		private unsafe void DeferredPause()
		{
			fixed (NativeMethods.SERVICE_STATUS* ptr = &this.status)
			{
				try
				{
					this.OnPause();
					this.WriteEventLogEntry(Res.GetString("PauseSuccessful"));
					this.status.currentState = 7;
				}
				catch (Exception ex)
				{
					this.status.currentState = 4;
					this.WriteEventLogEntry(Res.GetString("PauseFailed", new object[]
					{
						ex.ToString()
					}), EventLogEntryType.Error);
					throw;
				}
				finally
				{
					NativeMethods.SetServiceStatus(this.statusHandle, ptr);
				}
			}
		}

		private void DeferredPowerEvent(int eventType, IntPtr eventData)
		{
			try
			{
				bool flag = this.OnPowerEvent((PowerBroadcastStatus)eventType);
				this.WriteEventLogEntry(Res.GetString("PowerEventOK"));
			}
			catch (Exception ex)
			{
				this.WriteEventLogEntry(Res.GetString("PowerEventFailed", new object[]
				{
					ex.ToString()
				}), EventLogEntryType.Error);
				throw;
			}
		}

		private void DeferredSessionChange(int eventType, int sessionId)
		{
			try
			{
				this.OnSessionChange(new SessionChangeDescription((SessionChangeReason)eventType, sessionId));
			}
			catch (Exception ex)
			{
				this.WriteEventLogEntry(Res.GetString("SessionChangeFailed", new object[]
				{
					ex.ToString()
				}), EventLogEntryType.Error);
				throw;
			}
		}

		private unsafe void DeferredStop()
		{
			fixed (NativeMethods.SERVICE_STATUS* ptr = &this.status)
			{
				int currentState = this.status.currentState;
				this.status.checkPoint = 0;
				this.status.waitHint = 0;
				this.status.currentState = 3;
				NativeMethods.SetServiceStatus(this.statusHandle, ptr);
				try
				{
					this.OnStop();
					this.WriteEventLogEntry(Res.GetString("StopSuccessful"));
					this.status.currentState = 1;
					NativeMethods.SetServiceStatus(this.statusHandle, ptr);
					if (this.isServiceHosted)
					{
						try
						{
							AppDomain.Unload(AppDomain.CurrentDomain);
						}
						catch (CannotUnloadAppDomainException ex)
						{
							this.WriteEventLogEntry(Res.GetString("FailedToUnloadAppDomain", new object[]
							{
								AppDomain.CurrentDomain.FriendlyName,
								ex.Message
							}), EventLogEntryType.Error);
						}
					}
				}
				catch (Exception ex2)
				{
					this.status.currentState = currentState;
					NativeMethods.SetServiceStatus(this.statusHandle, ptr);
					this.WriteEventLogEntry(Res.GetString("StopFailed", new object[]
					{
						ex2.ToString()
					}), EventLogEntryType.Error);
					throw;
				}
			}
		}

		private unsafe void DeferredShutdown()
		{
			try
			{
				this.OnShutdown();
				this.WriteEventLogEntry(Res.GetString("ShutdownOK"));
				if (this.status.currentState == 7 || this.status.currentState == 4)
				{
					try
					{
						fixed (NativeMethods.SERVICE_STATUS* ptr = &this.status)
						{
							this.status.checkPoint = 0;
							this.status.waitHint = 0;
							this.status.currentState = 1;
							NativeMethods.SetServiceStatus(this.statusHandle, ptr);
							if (this.isServiceHosted)
							{
								try
								{
									AppDomain.Unload(AppDomain.CurrentDomain);
								}
								catch (CannotUnloadAppDomainException ex)
								{
									this.WriteEventLogEntry(Res.GetString("FailedToUnloadAppDomain", new object[]
									{
										AppDomain.CurrentDomain.FriendlyName,
										ex.Message
									}), EventLogEntryType.Error);
								}
							}
						}
					}
					finally
					{
						NativeMethods.SERVICE_STATUS* ptr = null;
					}
				}
			}
			catch (Exception ex2)
			{
				this.WriteEventLogEntry(Res.GetString("ShutdownFailed", new object[]
				{
					ex2.ToString()
				}), EventLogEntryType.Error);
				throw;
			}
		}

		protected virtual void OnCustomCommand(int command)
		{
		}

		public static void Run(ServiceBase[] services)
		{
			if (services == null || services.Length == 0)
			{
				throw new ArgumentException(Res.GetString("NoServices"));
			}
			if (Environment.OSVersion.Platform != PlatformID.Win32NT)
			{
				string @string = Res.GetString("CantRunOnWin9x");
				string string2 = Res.GetString("CantRunOnWin9xTitle");
				ServiceBase.LateBoundMessageBoxShow(@string, string2);
				return;
			}
			IntPtr intPtr = Marshal.AllocHGlobal((IntPtr)((services.Length + 1) * Marshal.SizeOf(typeof(NativeMethods.SERVICE_TABLE_ENTRY))));
			NativeMethods.SERVICE_TABLE_ENTRY[] array = new NativeMethods.SERVICE_TABLE_ENTRY[services.Length];
			bool multipleServices = services.Length > 1;
			IntPtr ptr = (IntPtr)0;
			for (int i = 0; i < services.Length; i++)
			{
				services[i].Initialize(multipleServices);
				array[i] = services[i].GetEntry();
				ptr = (IntPtr)((long)intPtr + (long)(Marshal.SizeOf(typeof(NativeMethods.SERVICE_TABLE_ENTRY)) * i));
				Marshal.StructureToPtr(array[i], ptr, true);
			}
			NativeMethods.SERVICE_TABLE_ENTRY sERVICE_TABLE_ENTRY = new NativeMethods.SERVICE_TABLE_ENTRY();
			sERVICE_TABLE_ENTRY.callback = null;
			sERVICE_TABLE_ENTRY.name = (IntPtr)0;
			ptr = (IntPtr)((long)intPtr + (long)(Marshal.SizeOf(typeof(NativeMethods.SERVICE_TABLE_ENTRY)) * services.Length));
			Marshal.StructureToPtr(sERVICE_TABLE_ENTRY, ptr, true);
			bool flag = NativeMethods.StartServiceCtrlDispatcher(intPtr);
			string text = "";
			if (!flag)
			{
				text = new Win32Exception().Message;
				string string3 = Res.GetString("CantStartFromCommandLine");
				if (Environment.UserInteractive)
				{
					string string4 = Res.GetString("CantStartFromCommandLineTitle");
					ServiceBase.LateBoundMessageBoxShow(string3, string4);
				}
				else
				{
					Console.WriteLine(string3);
				}
			}
			for (int j = 0; j < services.Length; j++)
			{
				ServiceBase serviceBase = services[j];
				serviceBase.Dispose();
				if (!flag && serviceBase.EventLog.Source.Length != 0)
				{
					serviceBase.WriteEventLogEntry(Res.GetString("StartFailed", new object[]
					{
						text
					}), EventLogEntryType.Error);
				}
			}
		}

		public static void Run(ServiceBase service)
		{
			if (service == null)
			{
				throw new ArgumentException(Res.GetString("NoServices"));
			}
			ServiceBase.Run(new ServiceBase[]
			{
				service
			});
		}

		public void Stop()
		{
			this.DeferredStop();
		}

		private void Initialize(bool multipleServices)
		{
			if (!this.initialized)
			{
				if (this.disposed)
				{
					throw new ObjectDisposedException(base.GetType().Name);
				}
				if (!multipleServices)
				{
					this.status.serviceType = 16;
				}
				else
				{
					this.status.serviceType = 32;
				}
				this.status.currentState = 2;
				this.status.controlsAccepted = 0;
				this.status.win32ExitCode = 0;
				this.status.serviceSpecificExitCode = 0;
				this.status.checkPoint = 0;
				this.status.waitHint = 0;
				this.mainCallback = new NativeMethods.ServiceMainCallback(this.ServiceMainCallback);
				this.commandCallback = new NativeMethods.ServiceControlCallback(this.ServiceCommandCallback);
				this.commandCallbackEx = new NativeMethods.ServiceControlCallbackEx(this.ServiceCommandCallbackEx);
				this.handleName = Marshal.StringToHGlobalUni(this.ServiceName);
				this.initialized = true;
			}
		}

		private NativeMethods.SERVICE_TABLE_ENTRY GetEntry()
		{
			NativeMethods.SERVICE_TABLE_ENTRY sERVICE_TABLE_ENTRY = new NativeMethods.SERVICE_TABLE_ENTRY();
			this.nameFrozen = true;
			sERVICE_TABLE_ENTRY.callback = this.mainCallback;
			sERVICE_TABLE_ENTRY.name = this.handleName;
			return sERVICE_TABLE_ENTRY;
		}

		private static void LateBoundMessageBoxShow(string message, string title)
		{
			int num = 0;
			if (ServiceBase.IsRTLResources)
			{
				num |= 1572864;
			}
			Type type = Type.GetType("System.Windows.Forms.MessageBox, System.Windows.Forms, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089");
			Type type2 = Type.GetType("System.Windows.Forms.MessageBoxButtons, System.Windows.Forms, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089");
			Type type3 = Type.GetType("System.Windows.Forms.MessageBoxIcon, System.Windows.Forms, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089");
			Type type4 = Type.GetType("System.Windows.Forms.MessageBoxDefaultButton, System.Windows.Forms, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089");
			Type type5 = Type.GetType("System.Windows.Forms.MessageBoxOptions, System.Windows.Forms, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089");
			type.InvokeMember("Show", BindingFlags.Static | BindingFlags.Public | BindingFlags.InvokeMethod, null, null, new object[]
			{
				message,
				title,
				Enum.ToObject(type2, 0),
				Enum.ToObject(type3, 0),
				Enum.ToObject(type4, 0),
				Enum.ToObject(type5, num)
			}, CultureInfo.InvariantCulture);
		}

		private int ServiceCommandCallbackEx(int command, int eventType, IntPtr eventData, IntPtr eventContext)
		{
			int result = 0;
			if (command != 13)
			{
				if (command != 14)
				{
					this.ServiceCommandCallback(command);
				}
				else
				{
					ServiceBase.DeferredHandlerDelegateAdvancedSession deferredHandlerDelegateAdvancedSession = new ServiceBase.DeferredHandlerDelegateAdvancedSession(this.DeferredSessionChange);
					NativeMethods.WTSSESSION_NOTIFICATION wTSSESSION_NOTIFICATION = new NativeMethods.WTSSESSION_NOTIFICATION();
					Marshal.PtrToStructure(eventData, wTSSESSION_NOTIFICATION);
					deferredHandlerDelegateAdvancedSession.BeginInvoke(eventType, wTSSESSION_NOTIFICATION.sessionId, null, null);
				}
			}
			else
			{
				ServiceBase.DeferredHandlerDelegateAdvanced deferredHandlerDelegateAdvanced = new ServiceBase.DeferredHandlerDelegateAdvanced(this.DeferredPowerEvent);
				deferredHandlerDelegateAdvanced.BeginInvoke(eventType, eventData, null, null);
			}
			return result;
		}

		private unsafe void ServiceCommandCallback(int command)
		{
			fixed (NativeMethods.SERVICE_STATUS* ptr = &this.status)
			{
				if (command == 4)
				{
					NativeMethods.SetServiceStatus(this.statusHandle, ptr);
				}
				else if (this.status.currentState != 5 && this.status.currentState != 2 && this.status.currentState != 3 && this.status.currentState != 6)
				{
					switch (command)
					{
					case 1:
					{
						int currentState = this.status.currentState;
						if (this.status.currentState == 7 || this.status.currentState == 4)
						{
							this.status.currentState = 3;
							NativeMethods.SetServiceStatus(this.statusHandle, ptr);
							this.status.currentState = currentState;
							ServiceBase.DeferredHandlerDelegate deferredHandlerDelegate = new ServiceBase.DeferredHandlerDelegate(this.DeferredStop);
							deferredHandlerDelegate.BeginInvoke(null, null);
							goto IL_1AA;
						}
						goto IL_1AA;
					}
					case 2:
						if (this.status.currentState == 4)
						{
							this.status.currentState = 6;
							NativeMethods.SetServiceStatus(this.statusHandle, ptr);
							ServiceBase.DeferredHandlerDelegate deferredHandlerDelegate2 = new ServiceBase.DeferredHandlerDelegate(this.DeferredPause);
							deferredHandlerDelegate2.BeginInvoke(null, null);
							goto IL_1AA;
						}
						goto IL_1AA;
					case 3:
						if (this.status.currentState == 7)
						{
							this.status.currentState = 5;
							NativeMethods.SetServiceStatus(this.statusHandle, ptr);
							ServiceBase.DeferredHandlerDelegate deferredHandlerDelegate3 = new ServiceBase.DeferredHandlerDelegate(this.DeferredContinue);
							deferredHandlerDelegate3.BeginInvoke(null, null);
							goto IL_1AA;
						}
						goto IL_1AA;
					case 5:
					{
						ServiceBase.DeferredHandlerDelegate deferredHandlerDelegate4 = new ServiceBase.DeferredHandlerDelegate(this.DeferredShutdown);
						deferredHandlerDelegate4.BeginInvoke(null, null);
						goto IL_1AA;
					}
					}
					ServiceBase.DeferredHandlerDelegateCommand deferredHandlerDelegateCommand = new ServiceBase.DeferredHandlerDelegateCommand(this.DeferredCustomCommand);
					deferredHandlerDelegateCommand.BeginInvoke(command, null, null);
				}
				IL_1AA:;
			}
		}

		private void ServiceQueuedMainCallback(object state)
		{
			string[] args = (string[])state;
			try
			{
				this.OnStart(args);
				this.WriteEventLogEntry(Res.GetString("StartSuccessful"));
				this.status.checkPoint = 0;
				this.status.waitHint = 0;
				this.status.currentState = 4;
			}
			catch (Exception ex)
			{
				this.WriteEventLogEntry(Res.GetString("StartFailed", new object[]
				{
					ex.ToString()
				}), EventLogEntryType.Error);
				this.status.currentState = 1;
			}
			this.startCompletedSignal.Set();
		}

		[EditorBrowsable(EditorBrowsableState.Never), ComVisible(false)]
		public unsafe void ServiceMainCallback(int argCount, IntPtr argPointer)
		{
			fixed (NativeMethods.SERVICE_STATUS* ptr = &this.status)
			{
				string[] array = null;
				if (argCount > 0)
				{
					char** ptr2 = (char**)argPointer.ToPointer();
					array = new string[argCount - 1];
					for (int i = 0; i < array.Length; i++)
					{
						ptr2 += sizeof(char*) / sizeof(char*);
						array[i] = Marshal.PtrToStringUni((IntPtr)(*(IntPtr*)ptr2));
					}
				}
				if (!this.initialized)
				{
					this.isServiceHosted = true;
					this.Initialize(true);
				}
				if (Environment.OSVersion.Version.Major >= 5)
				{
					this.statusHandle = NativeMethods.RegisterServiceCtrlHandlerEx(this.ServiceName, this.commandCallbackEx, (IntPtr)0);
				}
				else
				{
					this.statusHandle = NativeMethods.RegisterServiceCtrlHandler(this.ServiceName, this.commandCallback);
				}
				this.nameFrozen = true;
				if (this.statusHandle == (IntPtr)0)
				{
					string message = new Win32Exception().Message;
					this.WriteEventLogEntry(Res.GetString("StartFailed", new object[]
					{
						message
					}), EventLogEntryType.Error);
				}
				this.status.controlsAccepted = this.acceptedCommands;
				this.commandPropsFrozen = true;
				if ((this.status.controlsAccepted & 1) != 0)
				{
					this.status.controlsAccepted = (this.status.controlsAccepted | 4);
				}
				if (Environment.OSVersion.Version.Major < 5)
				{
					this.status.controlsAccepted = (this.status.controlsAccepted & -65);
				}
				this.status.currentState = 2;
				if (!NativeMethods.SetServiceStatus(this.statusHandle, ptr))
				{
					return;
				}
				this.startCompletedSignal = new ManualResetEvent(false);
				ThreadPool.QueueUserWorkItem(new WaitCallback(this.ServiceQueuedMainCallback), array);
				this.startCompletedSignal.WaitOne();
				if (!NativeMethods.SetServiceStatus(this.statusHandle, ptr))
				{
					this.WriteEventLogEntry(Res.GetString("StartFailed", new object[]
					{
						new Win32Exception().Message
					}), EventLogEntryType.Error);
					this.status.currentState = 1;
					NativeMethods.SetServiceStatus(this.statusHandle, ptr);
				}
			}
		}

		private void WriteEventLogEntry(string message)
		{
			try
			{
				if (this.AutoLog)
				{
					this.EventLog.WriteEntry(message);
				}
			}
			catch (StackOverflowException)
			{
				throw;
			}
			catch (OutOfMemoryException)
			{
				throw;
			}
			catch (ThreadAbortException)
			{
				throw;
			}
			catch
			{
			}
		}

		private void WriteEventLogEntry(string message, EventLogEntryType errorType)
		{
			try
			{
				if (this.AutoLog)
				{
					this.EventLog.WriteEntry(message, errorType);
				}
			}
			catch (StackOverflowException)
			{
				throw;
			}
			catch (OutOfMemoryException)
			{
				throw;
			}
			catch (ThreadAbortException)
			{
				throw;
			}
			catch
			{
			}
		}
	}
}
