using System;

namespace System.ServiceProcess
{
	public enum PowerBroadcastStatus
	{
		BatteryLow = 9,
		OemEvent = 11,
		PowerStatusChange = 10,
		QuerySuspend = 0,
		QuerySuspendFailed = 2,
		ResumeAutomatic = 18,
		ResumeCritical = 6,
		ResumeSuspend,
		Suspend = 4
	}
}
