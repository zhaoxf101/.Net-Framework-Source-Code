using Microsoft.Win32;
using System;
using System.Collections;
using System.ComponentModel;
using System.Configuration.Install;
using System.Runtime.InteropServices;

namespace System.Diagnostics
{
	public class EventLogInstaller : ComponentInstaller
	{
		private EventSourceCreationData sourceData = new EventSourceCreationData(null, null);

		private UninstallAction uninstallAction;

		[Editor("System.Windows.Forms.Design.FileNameEditor, System.Design, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a", "System.Drawing.Design.UITypeEditor, System.Drawing, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a"), TypeConverter("System.Diagnostics.Design.StringValueConverter, System.Design, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a"), ResDescription("Desc_CategoryResourceFile"), ComVisible(false)]
		public string CategoryResourceFile
		{
			get
			{
				return this.sourceData.CategoryResourceFile;
			}
			set
			{
				this.sourceData.CategoryResourceFile = value;
			}
		}

		[ResDescription("Desc_CategoryCount"), ComVisible(false)]
		public int CategoryCount
		{
			get
			{
				return this.sourceData.CategoryCount;
			}
			set
			{
				this.sourceData.CategoryCount = value;
			}
		}

		[TypeConverter("System.Diagnostics.Design.StringValueConverter, System.Design, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a"), ResDescription("Desc_Log")]
		public string Log
		{
			get
			{
				if (this.sourceData.LogName == null && this.sourceData.Source != null)
				{
					this.sourceData.LogName = EventLog.LogNameFromSourceName(this.sourceData.Source, ".");
				}
				return this.sourceData.LogName;
			}
			set
			{
				this.sourceData.LogName = value;
			}
		}

		[Editor("System.Windows.Forms.Design.FileNameEditor, System.Design, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a", "System.Drawing.Design.UITypeEditor, System.Drawing, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a"), TypeConverter("System.Diagnostics.Design.StringValueConverter, System.Design, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a"), ResDescription("Desc_MessageResourceFile"), ComVisible(false)]
		public string MessageResourceFile
		{
			get
			{
				return this.sourceData.MessageResourceFile;
			}
			set
			{
				this.sourceData.MessageResourceFile = value;
			}
		}

		[Editor("System.Windows.Forms.Design.FileNameEditor, System.Design, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a", "System.Drawing.Design.UITypeEditor, System.Drawing, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a"), TypeConverter("System.Diagnostics.Design.StringValueConverter, System.Design, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a"), ResDescription("Desc_ParameterResourceFile"), ComVisible(false)]
		public string ParameterResourceFile
		{
			get
			{
				return this.sourceData.ParameterResourceFile;
			}
			set
			{
				this.sourceData.ParameterResourceFile = value;
			}
		}

		[TypeConverter("System.Diagnostics.Design.StringValueConverter, System.Design, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a"), ResDescription("Desc_Source")]
		public string Source
		{
			get
			{
				return this.sourceData.Source;
			}
			set
			{
				this.sourceData.Source = value;
			}
		}

		[DefaultValue(UninstallAction.Remove), ResDescription("Desc_UninstallAction")]
		public UninstallAction UninstallAction
		{
			get
			{
				return this.uninstallAction;
			}
			set
			{
				if (!Enum.IsDefined(typeof(UninstallAction), value))
				{
					throw new InvalidEnumArgumentException("value", (int)value, typeof(UninstallAction));
				}
				this.uninstallAction = value;
			}
		}

		public override void CopyFromComponent(IComponent component)
		{
			EventLog eventLog = component as EventLog;
			if (eventLog == null)
			{
				throw new ArgumentException(Res.GetString("NotAnEventLog"));
			}
			if (eventLog.Log == null || eventLog.Log == string.Empty || eventLog.Source == null || eventLog.Source == string.Empty)
			{
				throw new ArgumentException(Res.GetString("IncompleteEventLog"));
			}
			this.Log = eventLog.Log;
			this.Source = eventLog.Source;
		}

		public override void Install(IDictionary stateSaver)
		{
			base.Install(stateSaver);
			base.Context.LogMessage(Res.GetString("CreatingEventLog", new object[]
			{
				this.Source,
				this.Log
			}));
			if (Environment.OSVersion.Platform != PlatformID.Win32NT)
			{
				throw new PlatformNotSupportedException(Res.GetString("WinNTRequired"));
			}
			stateSaver["baseInstalledAndPlatformOK"] = true;
			bool flag = EventLog.Exists(this.Log, ".");
			stateSaver["logExists"] = flag;
			bool flag2 = EventLog.SourceExists(this.Source, ".");
			stateSaver["alreadyRegistered"] = flag2;
			if (flag2)
			{
				string a = EventLog.LogNameFromSourceName(this.Source, ".");
				if (a == this.Log)
				{
					return;
				}
			}
			EventLog.CreateEventSource(this.sourceData);
		}

		public override bool IsEquivalentInstaller(ComponentInstaller otherInstaller)
		{
			EventLogInstaller eventLogInstaller = otherInstaller as EventLogInstaller;
			return eventLogInstaller != null && eventLogInstaller.Source == this.Source;
		}

		public override void Rollback(IDictionary savedState)
		{
			base.Rollback(savedState);
			base.Context.LogMessage(Res.GetString("RestoringEventLog", new object[]
			{
				this.Source
			}));
			if (savedState["baseInstalledAndPlatformOK"] != null)
			{
				if (!(bool)savedState["logExists"])
				{
					EventLog.Delete(this.Log, ".");
					return;
				}
				object obj = savedState["alreadyRegistered"];
				bool flag = obj != null && (bool)obj;
				if (!flag && EventLog.SourceExists(this.Source, "."))
				{
					EventLog.DeleteEventSource(this.Source, ".");
				}
			}
		}

		public override void Uninstall(IDictionary savedState)
		{
			base.Uninstall(savedState);
			if (this.UninstallAction == UninstallAction.Remove)
			{
				base.Context.LogMessage(Res.GetString("RemovingEventLog", new object[]
				{
					this.Source
				}));
				if (EventLog.SourceExists(this.Source, "."))
				{
					if (string.Compare(this.Log, this.Source, StringComparison.OrdinalIgnoreCase) != 0)
					{
						EventLog.DeleteEventSource(this.Source, ".");
					}
				}
				else
				{
					base.Context.LogMessage(Res.GetString("LocalSourceNotRegisteredWarning", new object[]
					{
						this.Source
					}));
				}
				RegistryKey registryKey = Registry.LocalMachine;
				RegistryKey registryKey2 = null;
				try
				{
					registryKey = registryKey.OpenSubKey("SYSTEM\\CurrentControlSet\\Services\\EventLog", false);
					if (registryKey != null)
					{
						registryKey2 = registryKey.OpenSubKey(this.Log, false);
					}
					if (registryKey2 != null)
					{
						string[] subKeyNames = registryKey2.GetSubKeyNames();
						if (subKeyNames == null || subKeyNames.Length == 0 || (subKeyNames.Length == 1 && string.Compare(subKeyNames[0], this.Log, StringComparison.OrdinalIgnoreCase) == 0))
						{
							base.Context.LogMessage(Res.GetString("DeletingEventLog", new object[]
							{
								this.Log
							}));
							EventLog.Delete(this.Log, ".");
						}
					}
				}
				finally
				{
					if (registryKey != null)
					{
						registryKey.Close();
					}
					if (registryKey2 != null)
					{
						registryKey2.Close();
					}
				}
			}
		}
	}
}
