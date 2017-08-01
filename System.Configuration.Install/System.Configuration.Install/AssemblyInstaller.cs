using System;
using System.Collections;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using System.Xml;

namespace System.Configuration.Install
{
	public class AssemblyInstaller : Installer
	{
		private Assembly assembly;

		private string[] commandLine;

		private bool useNewContext;

		private static bool helpPrinted;

		private bool initialized;

		[ResDescription("Desc_AssemblyInstaller_Assembly")]
		public Assembly Assembly
		{
			get
			{
				return this.assembly;
			}
			set
			{
				this.assembly = value;
			}
		}

		[ResDescription("Desc_AssemblyInstaller_CommandLine")]
		public string[] CommandLine
		{
			get
			{
				return this.commandLine;
			}
			set
			{
				this.commandLine = value;
			}
		}

		public override string HelpText
		{
			get
			{
				if (this.Path != null && this.Path.Length > 0)
				{
					base.Context = new InstallContext(null, new string[0]);
					if (!this.initialized)
					{
						this.InitializeFromAssembly();
					}
				}
				if (AssemblyInstaller.helpPrinted)
				{
					return base.HelpText;
				}
				AssemblyInstaller.helpPrinted = true;
				return Res.GetString("InstallAssemblyHelp") + "\r\n" + base.HelpText;
			}
		}

		[ResDescription("Desc_AssemblyInstaller_Path")]
		public string Path
		{
			get
			{
				if (this.assembly == null)
				{
					return null;
				}
				return this.assembly.Location;
			}
			set
			{
				if (value == null)
				{
					this.assembly = null;
				}
				this.assembly = Assembly.LoadFrom(value);
			}
		}

		[ResDescription("Desc_AssemblyInstaller_UseNewContext")]
		public bool UseNewContext
		{
			get
			{
				return this.useNewContext;
			}
			set
			{
				this.useNewContext = value;
			}
		}

		public AssemblyInstaller()
		{
		}

		public AssemblyInstaller(string fileName, string[] commandLine)
		{
			this.Path = System.IO.Path.GetFullPath(fileName);
			this.commandLine = commandLine;
			this.useNewContext = true;
		}

		public AssemblyInstaller(Assembly assembly, string[] commandLine)
		{
			this.Assembly = assembly;
			this.commandLine = commandLine;
			this.useNewContext = true;
		}

		public static void CheckIfInstallable(string assemblyName)
		{
			AssemblyInstaller assemblyInstaller = new AssemblyInstaller();
			assemblyInstaller.UseNewContext = false;
			assemblyInstaller.Path = assemblyName;
			assemblyInstaller.CommandLine = new string[0];
			assemblyInstaller.Context = new InstallContext(null, new string[0]);
			assemblyInstaller.InitializeFromAssembly();
			if (assemblyInstaller.Installers.Count == 0)
			{
				throw new InvalidOperationException(Res.GetString("InstallNoPublicInstallers", new object[]
				{
					assemblyName
				}));
			}
		}

		private InstallContext CreateAssemblyContext()
		{
			InstallContext installContext = new InstallContext(System.IO.Path.ChangeExtension(this.Path, ".InstallLog"), this.CommandLine);
			if (base.Context != null)
			{
				installContext.Parameters["logtoconsole"] = base.Context.Parameters["logtoconsole"];
			}
			installContext.Parameters["assemblypath"] = this.Path;
			return installContext;
		}

		private void InitializeFromAssembly()
		{
			Type[] array = null;
			try
			{
				array = AssemblyInstaller.GetInstallerTypes(this.assembly);
			}
			catch (Exception ex)
			{
				base.Context.LogMessage(Res.GetString("InstallException", new object[]
				{
					this.Path
				}));
				Installer.LogException(ex, base.Context);
				base.Context.LogMessage(Res.GetString("InstallAbort", new object[]
				{
					this.Path
				}));
				throw new InvalidOperationException(Res.GetString("InstallNoInstallerTypes", new object[]
				{
					this.Path
				}), ex);
			}
			if (array == null || array.Length == 0)
			{
				base.Context.LogMessage(Res.GetString("InstallNoPublicInstallers", new object[]
				{
					this.Path
				}));
				return;
			}
			for (int i = 0; i < array.Length; i++)
			{
				try
				{
					Installer value = (Installer)Activator.CreateInstance(array[i], BindingFlags.Instance | BindingFlags.Public | BindingFlags.CreateInstance, null, new object[0], null);
					base.Installers.Add(value);
				}
				catch (Exception ex2)
				{
					base.Context.LogMessage(Res.GetString("InstallCannotCreateInstance", new object[]
					{
						array[i].FullName
					}));
					Installer.LogException(ex2, base.Context);
					throw new InvalidOperationException(Res.GetString("InstallCannotCreateInstance", new object[]
					{
						array[i].FullName
					}), ex2);
				}
			}
			this.initialized = true;
		}

		private string GetInstallStatePath(string assemblyPath)
		{
			string text = base.Context.Parameters["InstallStateDir"];
			assemblyPath = System.IO.Path.ChangeExtension(assemblyPath, ".InstallState");
			string result;
			if (!string.IsNullOrEmpty(text))
			{
				result = System.IO.Path.Combine(text, System.IO.Path.GetFileName(assemblyPath));
			}
			else
			{
				result = assemblyPath;
			}
			return result;
		}

		public override void Commit(IDictionary savedState)
		{
			this.PrintStartText(Res.GetString("InstallActivityCommitting"));
			if (!this.initialized)
			{
				this.InitializeFromAssembly();
			}
			string installStatePath = this.GetInstallStatePath(this.Path);
			FileStream fileStream = new FileStream(installStatePath, FileMode.Open, FileAccess.Read);
			XmlReaderSettings xmlReaderSettings = new XmlReaderSettings();
			xmlReaderSettings.CheckCharacters = false;
			xmlReaderSettings.CloseInput = false;
			XmlReader xmlReader = null;
			if (fileStream != null)
			{
				xmlReader = XmlReader.Create(fileStream, xmlReaderSettings);
			}
			try
			{
				if (xmlReader != null)
				{
					NetDataContractSerializer netDataContractSerializer = new NetDataContractSerializer();
					savedState = (Hashtable)netDataContractSerializer.ReadObject(xmlReader);
				}
			}
			finally
			{
				if (xmlReader != null)
				{
					xmlReader.Close();
				}
				if (fileStream != null)
				{
					fileStream.Close();
				}
				if (base.Installers.Count == 0)
				{
					base.Context.LogMessage(Res.GetString("RemovingInstallState"));
					File.Delete(installStatePath);
				}
			}
			base.Commit(savedState);
		}

		private static Type[] GetInstallerTypes(Assembly assem)
		{
			ArrayList arrayList = new ArrayList();
			Module[] modules = assem.GetModules();
			for (int i = 0; i < modules.Length; i++)
			{
				Type[] types = modules[i].GetTypes();
				for (int j = 0; j < types.Length; j++)
				{
					if (typeof(Installer).IsAssignableFrom(types[j]) && !types[j].IsAbstract && types[j].IsPublic && ((RunInstallerAttribute)TypeDescriptor.GetAttributes(types[j])[typeof(RunInstallerAttribute)]).RunInstaller)
					{
						arrayList.Add(types[j]);
					}
				}
			}
			return (Type[])arrayList.ToArray(typeof(Type));
		}

		public override void Install(IDictionary savedState)
		{
			this.PrintStartText(Res.GetString("InstallActivityInstalling"));
			if (!this.initialized)
			{
				this.InitializeFromAssembly();
			}
			Hashtable hashtable = new Hashtable();
			savedState = hashtable;
			try
			{
				base.Install(savedState);
			}
			finally
			{
				FileStream fileStream = new FileStream(this.GetInstallStatePath(this.Path), FileMode.Create);
				XmlWriter xmlWriter = XmlWriter.Create(fileStream, new XmlWriterSettings
				{
					Encoding = Encoding.UTF8,
					CheckCharacters = false,
					CloseOutput = false
				});
				try
				{
					NetDataContractSerializer netDataContractSerializer = new NetDataContractSerializer();
					netDataContractSerializer.WriteObject(xmlWriter, savedState);
				}
				finally
				{
					xmlWriter.Close();
					fileStream.Close();
				}
			}
		}

		private void PrintStartText(string activity)
		{
			if (this.UseNewContext)
			{
				InstallContext installContext = this.CreateAssemblyContext();
				if (base.Context != null)
				{
					base.Context.LogMessage(Res.GetString("InstallLogContent", new object[]
					{
						this.Path
					}));
					base.Context.LogMessage(Res.GetString("InstallFileLocation", new object[]
					{
						installContext.Parameters["logfile"]
					}));
				}
				base.Context = installContext;
			}
			base.Context.LogMessage(string.Format(CultureInfo.InvariantCulture, activity, new object[]
			{
				this.Path
			}));
			base.Context.LogMessage(Res.GetString("InstallLogParameters"));
			if (base.Context.Parameters.Count == 0)
			{
				base.Context.LogMessage("   " + Res.GetString("InstallLogNone"));
			}
			IDictionaryEnumerator dictionaryEnumerator = (IDictionaryEnumerator)base.Context.Parameters.GetEnumerator();
			while (dictionaryEnumerator.MoveNext())
			{
				string text = (string)dictionaryEnumerator.Key;
				string str = (string)dictionaryEnumerator.Value;
				if (text.Equals("password", StringComparison.InvariantCultureIgnoreCase))
				{
					str = "********";
				}
				base.Context.LogMessage("   " + text + " = " + str);
			}
		}

		public override void Rollback(IDictionary savedState)
		{
			this.PrintStartText(Res.GetString("InstallActivityRollingBack"));
			if (!this.initialized)
			{
				this.InitializeFromAssembly();
			}
			string installStatePath = this.GetInstallStatePath(this.Path);
			FileStream fileStream = new FileStream(installStatePath, FileMode.Open, FileAccess.Read);
			XmlReaderSettings xmlReaderSettings = new XmlReaderSettings();
			xmlReaderSettings.CheckCharacters = false;
			xmlReaderSettings.CloseInput = false;
			XmlReader xmlReader = null;
			if (fileStream != null)
			{
				xmlReader = XmlReader.Create(fileStream, xmlReaderSettings);
			}
			try
			{
				if (xmlReader != null)
				{
					NetDataContractSerializer netDataContractSerializer = new NetDataContractSerializer();
					savedState = (Hashtable)netDataContractSerializer.ReadObject(xmlReader);
				}
			}
			finally
			{
				if (xmlReader != null)
				{
					xmlReader.Close();
				}
				if (fileStream != null)
				{
					fileStream.Close();
				}
			}
			try
			{
				base.Rollback(savedState);
			}
			finally
			{
				File.Delete(installStatePath);
			}
		}

		public override void Uninstall(IDictionary savedState)
		{
			this.PrintStartText(Res.GetString("InstallActivityUninstalling"));
			if (!this.initialized)
			{
				this.InitializeFromAssembly();
			}
			string installStatePath = this.GetInstallStatePath(this.Path);
			if (installStatePath != null && File.Exists(installStatePath))
			{
				FileStream fileStream = new FileStream(installStatePath, FileMode.Open, FileAccess.Read);
				XmlReaderSettings xmlReaderSettings = new XmlReaderSettings();
				xmlReaderSettings.CheckCharacters = false;
				xmlReaderSettings.CloseInput = false;
				XmlReader xmlReader = null;
				if (fileStream != null)
				{
					xmlReader = XmlReader.Create(fileStream, xmlReaderSettings);
				}
				try
				{
					if (xmlReader != null)
					{
						NetDataContractSerializer netDataContractSerializer = new NetDataContractSerializer();
						savedState = (Hashtable)netDataContractSerializer.ReadObject(xmlReader);
					}
					goto IL_C6;
				}
				catch
				{
					base.Context.LogMessage(Res.GetString("InstallSavedStateFileCorruptedWarning", new object[]
					{
						this.Path,
						installStatePath
					}));
					savedState = null;
					goto IL_C6;
				}
				finally
				{
					if (xmlReader != null)
					{
						xmlReader.Close();
					}
					if (fileStream != null)
					{
						fileStream.Close();
					}
				}
			}
			savedState = null;
			IL_C6:
			base.Uninstall(savedState);
			if (installStatePath != null && installStatePath.Length != 0)
			{
				try
				{
					File.Delete(installStatePath);
				}
				catch
				{
					throw new InvalidOperationException(Res.GetString("InstallUnableDeleteFile", new object[]
					{
						installStatePath
					}));
				}
			}
		}
	}
}
