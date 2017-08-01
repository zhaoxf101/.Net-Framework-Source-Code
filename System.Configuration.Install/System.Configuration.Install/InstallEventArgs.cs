using System;
using System.Collections;

namespace System.Configuration.Install
{
	public class InstallEventArgs : EventArgs
	{
		private IDictionary savedState;

		public IDictionary SavedState
		{
			get
			{
				return this.savedState;
			}
		}

		public InstallEventArgs()
		{
		}

		public InstallEventArgs(IDictionary savedState)
		{
			this.savedState = savedState;
		}
	}
}
