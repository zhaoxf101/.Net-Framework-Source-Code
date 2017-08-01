using System;

namespace System.ServiceProcess
{
	[Flags]
	public enum ServiceType
	{
		Adapter = 4,
		FileSystemDriver = 2,
		InteractiveProcess = 256,
		KernelDriver = 1,
		RecognizerDriver = 8,
		Win32OwnProcess = 16,
		Win32ShareProcess = 32
	}
}
