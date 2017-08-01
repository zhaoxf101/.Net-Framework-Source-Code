using System;
using System.Diagnostics;

namespace System.ComponentModel
{
	internal static class CompModSwitches
	{
		private static TraceSwitch installerDesign;

		public static TraceSwitch InstallerDesign
		{
			get
			{
				if (CompModSwitches.installerDesign == null)
				{
					CompModSwitches.installerDesign = new TraceSwitch("InstallerDesign", "Enable tracing for design-time code for installers");
				}
				return CompModSwitches.installerDesign;
			}
		}
	}
}
