using System;

namespace System.ServiceProcess
{
	public enum ServiceStartMode
	{
		Manual = 3,
		Automatic = 2,
		Disabled = 4,
		Boot = 0,
		System
	}
}
