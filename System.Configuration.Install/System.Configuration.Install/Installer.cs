using System;
using System.Collections;
using System.ComponentModel;
using System.Text;

namespace System.Configuration.Install
{
	[DefaultEvent("AfterInstall")]
	public class Installer : Component
	{
		private InstallerCollection installers;

		private InstallContext context;

		internal Installer parent;

		private InstallEventHandler afterCommitHandler;

		private InstallEventHandler afterInstallHandler;

		private InstallEventHandler afterRollbackHandler;

		private InstallEventHandler afterUninstallHandler;

		private InstallEventHandler beforeCommitHandler;

		private InstallEventHandler beforeInstallHandler;

		private InstallEventHandler beforeRollbackHandler;

		private InstallEventHandler beforeUninstallHandler;

		private const string wrappedExceptionSource = "WrappedExceptionSource";

		public event InstallEventHandler Committed
		{
			add
			{
				this.afterCommitHandler = (InstallEventHandler)Delegate.Combine(this.afterCommitHandler, value);
			}
			remove
			{
				this.afterCommitHandler = (InstallEventHandler)Delegate.Remove(this.afterCommitHandler, value);
			}
		}

		public event InstallEventHandler AfterInstall
		{
			add
			{
				this.afterInstallHandler = (InstallEventHandler)Delegate.Combine(this.afterInstallHandler, value);
			}
			remove
			{
				this.afterInstallHandler = (InstallEventHandler)Delegate.Remove(this.afterInstallHandler, value);
			}
		}

		public event InstallEventHandler AfterRollback
		{
			add
			{
				this.afterRollbackHandler = (InstallEventHandler)Delegate.Combine(this.afterRollbackHandler, value);
			}
			remove
			{
				this.afterRollbackHandler = (InstallEventHandler)Delegate.Remove(this.afterRollbackHandler, value);
			}
		}

		public event InstallEventHandler AfterUninstall
		{
			add
			{
				this.afterUninstallHandler = (InstallEventHandler)Delegate.Combine(this.afterUninstallHandler, value);
			}
			remove
			{
				this.afterUninstallHandler = (InstallEventHandler)Delegate.Remove(this.afterUninstallHandler, value);
			}
		}

		public event InstallEventHandler Committing
		{
			add
			{
				this.beforeCommitHandler = (InstallEventHandler)Delegate.Combine(this.beforeCommitHandler, value);
			}
			remove
			{
				this.beforeCommitHandler = (InstallEventHandler)Delegate.Remove(this.beforeCommitHandler, value);
			}
		}

		public event InstallEventHandler BeforeInstall
		{
			add
			{
				this.beforeInstallHandler = (InstallEventHandler)Delegate.Combine(this.beforeInstallHandler, value);
			}
			remove
			{
				this.beforeInstallHandler = (InstallEventHandler)Delegate.Remove(this.beforeInstallHandler, value);
			}
		}

		public event InstallEventHandler BeforeRollback
		{
			add
			{
				this.beforeRollbackHandler = (InstallEventHandler)Delegate.Combine(this.beforeRollbackHandler, value);
			}
			remove
			{
				this.beforeRollbackHandler = (InstallEventHandler)Delegate.Remove(this.beforeRollbackHandler, value);
			}
		}

		public event InstallEventHandler BeforeUninstall
		{
			add
			{
				this.beforeUninstallHandler = (InstallEventHandler)Delegate.Combine(this.beforeUninstallHandler, value);
			}
			remove
			{
				this.beforeUninstallHandler = (InstallEventHandler)Delegate.Remove(this.beforeUninstallHandler, value);
			}
		}

		[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public InstallContext Context
		{
			get
			{
				return this.context;
			}
			set
			{
				this.context = value;
			}
		}

		[ResDescription("Desc_Installer_HelpText")]
		public virtual string HelpText
		{
			get
			{
				StringBuilder stringBuilder = new StringBuilder();
				for (int i = 0; i < this.Installers.Count; i++)
				{
					string helpText = this.Installers[i].HelpText;
					if (helpText.Length > 0)
					{
						stringBuilder.Append("\r\n");
						stringBuilder.Append(helpText);
					}
				}
				return stringBuilder.ToString();
			}
		}

		[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
		public InstallerCollection Installers
		{
			get
			{
				if (this.installers == null)
				{
					this.installers = new InstallerCollection(this);
				}
				return this.installers;
			}
		}

		[Browsable(true), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden), TypeConverter(typeof(InstallerParentConverter)), ResDescription("Desc_Installer_Parent")]
		public Installer Parent
		{
			get
			{
				return this.parent;
			}
			set
			{
				if (value == this)
				{
					throw new InvalidOperationException(Res.GetString("InstallBadParent"));
				}
				if (value == this.parent)
				{
					return;
				}
				if (value != null && this.InstallerTreeContains(value))
				{
					throw new InvalidOperationException(Res.GetString("InstallRecursiveParent"));
				}
				if (this.parent != null)
				{
					int num = this.parent.Installers.IndexOf(this);
					if (num != -1)
					{
						this.parent.Installers.RemoveAt(num);
					}
				}
				this.parent = value;
				if (this.parent != null && !this.parent.Installers.Contains(this))
				{
					this.parent.Installers.Add(this);
				}
			}
		}

		internal bool InstallerTreeContains(Installer target)
		{
			if (this.Installers.Contains(target))
			{
				return true;
			}
			foreach (Installer installer in this.Installers)
			{
				if (installer.InstallerTreeContains(target))
				{
					return true;
				}
			}
			return false;
		}

		public virtual void Commit(IDictionary savedState)
		{
			if (savedState == null)
			{
				throw new ArgumentException(Res.GetString("InstallNullParameter", new object[]
				{
					"savedState"
				}));
			}
			if (savedState["_reserved_lastInstallerAttempted"] == null || savedState["_reserved_nestedSavedStates"] == null)
			{
				throw new ArgumentException(Res.GetString("InstallDictionaryMissingValues", new object[]
				{
					"savedState"
				}));
			}
			Exception ex = null;
			try
			{
				this.OnCommitting(savedState);
			}
			catch (Exception ex2)
			{
				this.WriteEventHandlerError(Res.GetString("InstallSeverityWarning"), "OnCommitting", ex2);
				this.Context.LogMessage(Res.GetString("InstallCommitException"));
				ex = ex2;
			}
			int num = (int)savedState["_reserved_lastInstallerAttempted"];
			IDictionary[] array = (IDictionary[])savedState["_reserved_nestedSavedStates"];
			if (num + 1 != array.Length || num >= this.Installers.Count)
			{
				throw new ArgumentException(Res.GetString("InstallDictionaryCorrupted", new object[]
				{
					"savedState"
				}));
			}
			for (int i = 0; i < this.Installers.Count; i++)
			{
				this.Installers[i].Context = this.Context;
			}
			for (int j = 0; j <= num; j++)
			{
				try
				{
					this.Installers[j].Commit(array[j]);
				}
				catch (Exception ex3)
				{
					if (!this.IsWrappedException(ex3))
					{
						this.Context.LogMessage(Res.GetString("InstallLogCommitException", new object[]
						{
							this.Installers[j].ToString()
						}));
						Installer.LogException(ex3, this.Context);
						this.Context.LogMessage(Res.GetString("InstallCommitException"));
					}
					ex = ex3;
				}
			}
			savedState["_reserved_nestedSavedStates"] = array;
			savedState.Remove("_reserved_lastInstallerAttempted");
			try
			{
				this.OnCommitted(savedState);
			}
			catch (Exception ex4)
			{
				this.WriteEventHandlerError(Res.GetString("InstallSeverityWarning"), "OnCommitted", ex4);
				this.Context.LogMessage(Res.GetString("InstallCommitException"));
				ex = ex4;
			}
			if (ex != null)
			{
				Exception ex5 = ex;
				if (!this.IsWrappedException(ex))
				{
					ex5 = new InstallException(Res.GetString("InstallCommitException"), ex);
					ex5.Source = "WrappedExceptionSource";
				}
				throw ex5;
			}
		}

		public virtual void Install(IDictionary stateSaver)
		{
			if (stateSaver == null)
			{
				throw new ArgumentException(Res.GetString("InstallNullParameter", new object[]
				{
					"stateSaver"
				}));
			}
			try
			{
				this.OnBeforeInstall(stateSaver);
			}
			catch (Exception ex)
			{
				this.WriteEventHandlerError(Res.GetString("InstallSeverityError"), "OnBeforeInstall", ex);
				throw new InvalidOperationException(Res.GetString("InstallEventException", new object[]
				{
					"OnBeforeInstall",
					base.GetType().FullName
				}), ex);
			}
			int num = -1;
			ArrayList arrayList = new ArrayList();
			try
			{
				for (int i = 0; i < this.Installers.Count; i++)
				{
					this.Installers[i].Context = this.Context;
				}
				for (int j = 0; j < this.Installers.Count; j++)
				{
					Installer installer = this.Installers[j];
					IDictionary dictionary = new Hashtable();
					try
					{
						num = j;
						installer.Install(dictionary);
					}
					finally
					{
						arrayList.Add(dictionary);
					}
				}
			}
			finally
			{
				stateSaver.Add("_reserved_lastInstallerAttempted", num);
				stateSaver.Add("_reserved_nestedSavedStates", arrayList.ToArray(typeof(IDictionary)));
			}
			try
			{
				this.OnAfterInstall(stateSaver);
			}
			catch (Exception ex2)
			{
				this.WriteEventHandlerError(Res.GetString("InstallSeverityError"), "OnAfterInstall", ex2);
				throw new InvalidOperationException(Res.GetString("InstallEventException", new object[]
				{
					"OnAfterInstall",
					base.GetType().FullName
				}), ex2);
			}
		}

		internal static void LogException(Exception e, InstallContext context)
		{
			bool flag = true;
			while (e != null)
			{
				if (flag)
				{
					context.LogMessage(e.GetType().FullName + ": " + e.Message);
					flag = false;
				}
				else
				{
					context.LogMessage(Res.GetString("InstallLogInner", new object[]
					{
						e.GetType().FullName,
						e.Message
					}));
				}
				if (context.IsParameterTrue("showcallstack"))
				{
					context.LogMessage(e.StackTrace);
				}
				e = e.InnerException;
			}
		}

		private bool IsWrappedException(Exception e)
		{
			return e is InstallException && e.Source == "WrappedExceptionSource" && e.TargetSite.ReflectedType == typeof(Installer);
		}

		protected virtual void OnCommitted(IDictionary savedState)
		{
			if (this.afterCommitHandler != null)
			{
				this.afterCommitHandler(this, new InstallEventArgs(savedState));
			}
		}

		protected virtual void OnAfterInstall(IDictionary savedState)
		{
			if (this.afterInstallHandler != null)
			{
				this.afterInstallHandler(this, new InstallEventArgs(savedState));
			}
		}

		protected virtual void OnAfterRollback(IDictionary savedState)
		{
			if (this.afterRollbackHandler != null)
			{
				this.afterRollbackHandler(this, new InstallEventArgs(savedState));
			}
		}

		protected virtual void OnAfterUninstall(IDictionary savedState)
		{
			if (this.afterUninstallHandler != null)
			{
				this.afterUninstallHandler(this, new InstallEventArgs(savedState));
			}
		}

		protected virtual void OnCommitting(IDictionary savedState)
		{
			if (this.beforeCommitHandler != null)
			{
				this.beforeCommitHandler(this, new InstallEventArgs(savedState));
			}
		}

		protected virtual void OnBeforeInstall(IDictionary savedState)
		{
			if (this.beforeInstallHandler != null)
			{
				this.beforeInstallHandler(this, new InstallEventArgs(savedState));
			}
		}

		protected virtual void OnBeforeRollback(IDictionary savedState)
		{
			if (this.beforeRollbackHandler != null)
			{
				this.beforeRollbackHandler(this, new InstallEventArgs(savedState));
			}
		}

		protected virtual void OnBeforeUninstall(IDictionary savedState)
		{
			if (this.beforeUninstallHandler != null)
			{
				this.beforeUninstallHandler(this, new InstallEventArgs(savedState));
			}
		}

		public virtual void Rollback(IDictionary savedState)
		{
			if (savedState == null)
			{
				throw new ArgumentException(Res.GetString("InstallNullParameter", new object[]
				{
					"savedState"
				}));
			}
			if (savedState["_reserved_lastInstallerAttempted"] == null || savedState["_reserved_nestedSavedStates"] == null)
			{
				throw new ArgumentException(Res.GetString("InstallDictionaryMissingValues", new object[]
				{
					"savedState"
				}));
			}
			Exception ex = null;
			try
			{
				this.OnBeforeRollback(savedState);
			}
			catch (Exception ex2)
			{
				this.WriteEventHandlerError(Res.GetString("InstallSeverityWarning"), "OnBeforeRollback", ex2);
				this.Context.LogMessage(Res.GetString("InstallRollbackException"));
				ex = ex2;
			}
			int num = (int)savedState["_reserved_lastInstallerAttempted"];
			IDictionary[] array = (IDictionary[])savedState["_reserved_nestedSavedStates"];
			if (num + 1 != array.Length || num >= this.Installers.Count)
			{
				throw new ArgumentException(Res.GetString("InstallDictionaryCorrupted", new object[]
				{
					"savedState"
				}));
			}
			for (int i = this.Installers.Count - 1; i >= 0; i--)
			{
				this.Installers[i].Context = this.Context;
			}
			for (int j = num; j >= 0; j--)
			{
				try
				{
					this.Installers[j].Rollback(array[j]);
				}
				catch (Exception ex3)
				{
					if (!this.IsWrappedException(ex3))
					{
						this.Context.LogMessage(Res.GetString("InstallLogRollbackException", new object[]
						{
							this.Installers[j].ToString()
						}));
						Installer.LogException(ex3, this.Context);
						this.Context.LogMessage(Res.GetString("InstallRollbackException"));
					}
					ex = ex3;
				}
			}
			try
			{
				this.OnAfterRollback(savedState);
			}
			catch (Exception ex4)
			{
				this.WriteEventHandlerError(Res.GetString("InstallSeverityWarning"), "OnAfterRollback", ex4);
				this.Context.LogMessage(Res.GetString("InstallRollbackException"));
				ex = ex4;
			}
			if (ex != null)
			{
				Exception ex5 = ex;
				if (!this.IsWrappedException(ex))
				{
					ex5 = new InstallException(Res.GetString("InstallRollbackException"), ex);
					ex5.Source = "WrappedExceptionSource";
				}
				throw ex5;
			}
		}

		public virtual void Uninstall(IDictionary savedState)
		{
			Exception ex = null;
			try
			{
				this.OnBeforeUninstall(savedState);
			}
			catch (Exception ex2)
			{
				this.WriteEventHandlerError(Res.GetString("InstallSeverityWarning"), "OnBeforeUninstall", ex2);
				this.Context.LogMessage(Res.GetString("InstallUninstallException"));
				ex = ex2;
			}
			IDictionary[] array;
			if (savedState != null)
			{
				array = (IDictionary[])savedState["_reserved_nestedSavedStates"];
				if (array == null || array.Length != this.Installers.Count)
				{
					throw new ArgumentException(Res.GetString("InstallDictionaryCorrupted", new object[]
					{
						"savedState"
					}));
				}
			}
			else
			{
				array = new IDictionary[this.Installers.Count];
			}
			for (int i = this.Installers.Count - 1; i >= 0; i--)
			{
				this.Installers[i].Context = this.Context;
			}
			for (int j = this.Installers.Count - 1; j >= 0; j--)
			{
				try
				{
					this.Installers[j].Uninstall(array[j]);
				}
				catch (Exception ex3)
				{
					if (!this.IsWrappedException(ex3))
					{
						this.Context.LogMessage(Res.GetString("InstallLogUninstallException", new object[]
						{
							this.Installers[j].ToString()
						}));
						Installer.LogException(ex3, this.Context);
						this.Context.LogMessage(Res.GetString("InstallUninstallException"));
					}
					ex = ex3;
				}
			}
			try
			{
				this.OnAfterUninstall(savedState);
			}
			catch (Exception ex4)
			{
				this.WriteEventHandlerError(Res.GetString("InstallSeverityWarning"), "OnAfterUninstall", ex4);
				this.Context.LogMessage(Res.GetString("InstallUninstallException"));
				ex = ex4;
			}
			if (ex != null)
			{
				Exception ex5 = ex;
				if (!this.IsWrappedException(ex))
				{
					ex5 = new InstallException(Res.GetString("InstallUninstallException"), ex);
					ex5.Source = "WrappedExceptionSource";
				}
				throw ex5;
			}
		}

		private void WriteEventHandlerError(string severity, string eventName, Exception e)
		{
			this.Context.LogMessage(Res.GetString("InstallLogError", new object[]
			{
				severity,
				eventName,
				base.GetType().FullName
			}));
			Installer.LogException(e, this.Context);
		}
	}
}
