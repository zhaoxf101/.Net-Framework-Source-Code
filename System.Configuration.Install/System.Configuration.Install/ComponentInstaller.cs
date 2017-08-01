using System;
using System.ComponentModel;

namespace System.Configuration.Install
{
	public abstract class ComponentInstaller : Installer
	{
		public abstract void CopyFromComponent(IComponent component);

		public virtual bool IsEquivalentInstaller(ComponentInstaller otherInstaller)
		{
			return false;
		}
	}
}
