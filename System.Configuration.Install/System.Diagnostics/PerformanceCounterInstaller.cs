using Microsoft.Win32;
using System;
using System.Collections;
using System.ComponentModel;
using System.Configuration.Install;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace System.Diagnostics
{
	public class PerformanceCounterInstaller : ComponentInstaller
	{
		private const string ServicePath = "SYSTEM\\CurrentControlSet\\Services";

		private const string PerfShimName = "netfxperf.dll";

		private const string PerfShimFullNameSuffix = "\\netfxperf.dll";

		private string categoryName = string.Empty;

		private CounterCreationDataCollection counters = new CounterCreationDataCollection();

		private string categoryHelp = string.Empty;

		private UninstallAction uninstallAction;

		private PerformanceCounterCategoryType categoryType = PerformanceCounterCategoryType.Unknown;

		[DefaultValue(""), TypeConverter("System.Diagnostics.Design.StringValueConverter, System.Design, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a"), ResDescription("PCCategoryName")]
		public string CategoryName
		{
			get
			{
				return this.categoryName;
			}
			set
			{
				if (value == null)
				{
					throw new ArgumentNullException("value");
				}
				PerformanceCounterInstaller.CheckValidCategory(value);
				this.categoryName = value;
			}
		}

		[DefaultValue(""), ResDescription("PCI_CategoryHelp")]
		public string CategoryHelp
		{
			get
			{
				return this.categoryHelp;
			}
			set
			{
				if (value == null)
				{
					throw new ArgumentNullException("value");
				}
				this.categoryHelp = value;
			}
		}

		[DefaultValue(PerformanceCounterCategoryType.Unknown), ResDescription("PCI_IsMultiInstance"), ComVisible(false)]
		public PerformanceCounterCategoryType CategoryType
		{
			get
			{
				return this.categoryType;
			}
			set
			{
				if (value < PerformanceCounterCategoryType.Unknown || value > PerformanceCounterCategoryType.MultiInstance)
				{
					throw new InvalidEnumArgumentException("value", (int)value, typeof(PerformanceCounterCategoryType));
				}
				this.categoryType = value;
			}
		}

		[DesignerSerializationVisibility(DesignerSerializationVisibility.Content), ResDescription("PCI_Counters")]
		public CounterCreationDataCollection Counters
		{
			get
			{
				return this.counters;
			}
		}

		[DefaultValue(UninstallAction.Remove), ResDescription("PCI_UninstallAction")]
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
			if (!(component is PerformanceCounter))
			{
				throw new ArgumentException(Res.GetString("NotAPerformanceCounter"));
			}
			PerformanceCounter performanceCounter = (PerformanceCounter)component;
			if (performanceCounter.CategoryName == null || performanceCounter.CategoryName.Length == 0)
			{
				throw new ArgumentException(Res.GetString("IncompletePerformanceCounter"));
			}
			if (!this.CategoryName.Equals(performanceCounter.CategoryName) && !string.IsNullOrEmpty(this.CategoryName))
			{
				throw new ArgumentException(Res.GetString("NewCategory"));
			}
			PerformanceCounterType counterType = PerformanceCounterType.NumberOfItems32;
			string counterHelp = string.Empty;
			if (string.IsNullOrEmpty(this.CategoryName))
			{
				this.CategoryName = performanceCounter.CategoryName;
			}
			if (Environment.OSVersion.Platform == PlatformID.Win32NT)
			{
				string machineName = performanceCounter.MachineName;
				if (PerformanceCounterCategory.Exists(this.CategoryName, machineName))
				{
					string name = "SYSTEM\\CurrentControlSet\\Services\\" + this.CategoryName + "\\Performance";
					RegistryKey registryKey = null;
					try
					{
						if (machineName == "." || string.Compare(machineName, SystemInformation.ComputerName, StringComparison.OrdinalIgnoreCase) == 0)
						{
							registryKey = Registry.LocalMachine.OpenSubKey(name);
						}
						else
						{
							RegistryKey registryKey2 = RegistryKey.OpenRemoteBaseKey(RegistryHive.LocalMachine, "\\\\" + machineName);
							registryKey = registryKey2.OpenSubKey(name);
						}
						if (registryKey == null)
						{
							throw new ArgumentException(Res.GetString("NotCustomPerformanceCategory"));
						}
						object value = registryKey.GetValue("Library", null, RegistryValueOptions.DoNotExpandEnvironmentNames);
						if (value == null || !(value is string) || (string.Compare((string)value, "netfxperf.dll", StringComparison.OrdinalIgnoreCase) != 0 && !((string)value).EndsWith("\\netfxperf.dll", StringComparison.OrdinalIgnoreCase)))
						{
							throw new ArgumentException(Res.GetString("NotCustomPerformanceCategory"));
						}
						PerformanceCounterCategory performanceCounterCategory = new PerformanceCounterCategory(this.CategoryName, machineName);
						this.CategoryHelp = performanceCounterCategory.CategoryHelp;
						if (performanceCounterCategory.CounterExists(performanceCounter.CounterName))
						{
							counterType = performanceCounter.CounterType;
							counterHelp = performanceCounter.CounterHelp;
						}
						this.CategoryType = performanceCounterCategory.CategoryType;
					}
					finally
					{
						if (registryKey != null)
						{
							registryKey.Close();
						}
					}
				}
			}
			CounterCreationData value2 = new CounterCreationData(performanceCounter.CounterName, counterHelp, counterType);
			this.Counters.Add(value2);
		}

		private void DoRollback(IDictionary state)
		{
			base.Context.LogMessage(Res.GetString("RestoringPerformanceCounter", new object[]
			{
				this.CategoryName
			}));
			using (RegistryKey registryKey = Registry.LocalMachine.OpenSubKey("SYSTEM\\CurrentControlSet\\Services", true))
			{
				RegistryKey registryKey2;
				if ((bool)state["categoryKeyExisted"])
				{
					registryKey2 = registryKey.OpenSubKey(this.CategoryName, true);
					if (registryKey2 == null)
					{
						registryKey2 = registryKey.CreateSubKey(this.CategoryName);
					}
					registryKey2.DeleteSubKeyTree("Performance");
					SerializableRegistryKey serializableRegistryKey = (SerializableRegistryKey)state["performanceKeyData"];
					if (serializableRegistryKey != null)
					{
						RegistryKey registryKey3 = registryKey2.CreateSubKey("Performance");
						serializableRegistryKey.CopyToRegistry(registryKey3);
						registryKey3.Close();
					}
					registryKey2.DeleteSubKeyTree("Linkage");
					SerializableRegistryKey serializableRegistryKey2 = (SerializableRegistryKey)state["linkageKeyData"];
					if (serializableRegistryKey2 != null)
					{
						RegistryKey registryKey4 = registryKey2.CreateSubKey("Linkage");
						serializableRegistryKey2.CopyToRegistry(registryKey4);
						registryKey4.Close();
					}
				}
				else
				{
					registryKey2 = registryKey.OpenSubKey(this.CategoryName);
					if (registryKey2 != null)
					{
						registryKey2.Close();
						registryKey2 = null;
						registryKey.DeleteSubKeyTree(this.CategoryName);
					}
				}
				if (registryKey2 != null)
				{
					registryKey2.Close();
				}
			}
		}

		public override void Install(IDictionary stateSaver)
		{
			base.Install(stateSaver);
			base.Context.LogMessage(Res.GetString("CreatingPerformanceCounter", new object[]
			{
				this.CategoryName
			}));
			RegistryKey registryKey = null;
			RegistryKey registryKey2 = Registry.LocalMachine.OpenSubKey("SYSTEM\\CurrentControlSet\\Services", true);
			stateSaver["categoryKeyExisted"] = false;
			try
			{
				if (registryKey2 != null)
				{
					registryKey = registryKey2.OpenSubKey(this.CategoryName, true);
					if (registryKey != null)
					{
						stateSaver["categoryKeyExisted"] = true;
						RegistryKey registryKey3 = registryKey.OpenSubKey("Performance");
						if (registryKey3 != null)
						{
							stateSaver["performanceKeyData"] = new SerializableRegistryKey(registryKey3);
							registryKey3.Close();
							registryKey.DeleteSubKeyTree("Performance");
						}
						registryKey3 = registryKey.OpenSubKey("Linkage");
						if (registryKey3 != null)
						{
							stateSaver["linkageKeyData"] = new SerializableRegistryKey(registryKey3);
							registryKey3.Close();
							registryKey.DeleteSubKeyTree("Linkage");
						}
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
			if (PerformanceCounterCategory.Exists(this.CategoryName))
			{
				PerformanceCounterCategory.Delete(this.CategoryName);
			}
			PerformanceCounterCategory.Create(this.CategoryName, this.CategoryHelp, this.categoryType, this.Counters);
		}

		public override void Rollback(IDictionary savedState)
		{
			base.Rollback(savedState);
			this.DoRollback(savedState);
		}

		public override void Uninstall(IDictionary savedState)
		{
			base.Uninstall(savedState);
			if (this.UninstallAction == UninstallAction.Remove)
			{
				base.Context.LogMessage(Res.GetString("RemovingPerformanceCounter", new object[]
				{
					this.CategoryName
				}));
				PerformanceCounterCategory.Delete(this.CategoryName);
			}
		}

		internal static void CheckValidCategory(string categoryName)
		{
			if (categoryName == null)
			{
				throw new ArgumentNullException("categoryName");
			}
			if (!PerformanceCounterInstaller.CheckValidId(categoryName))
			{
				throw new ArgumentException(Res.GetString("PerfInvalidCategoryName", new object[]
				{
					1,
					253
				}));
			}
		}

		internal static bool CheckValidId(string id)
		{
			if (id.Length == 0 || id.Length > 253)
			{
				return false;
			}
			for (int i = 0; i < id.Length; i++)
			{
				char c = id[i];
				if ((i == 0 || i == id.Length - 1) && c == ' ')
				{
					return false;
				}
				if (c == '"')
				{
					return false;
				}
				if (char.IsControl(c))
				{
					return false;
				}
			}
			return true;
		}
	}
}
