using System;
using System.Runtime.InteropServices;

namespace Shared;

internal class PowerState
{
	public static bool Sleep() => Hibernate(hibernate: false);
	public static bool Hibernate() => Hibernate(hibernate: true);

	private static bool Hibernate(bool hibernate)
	{
		bool success = SetSuspendState(
			hibernate: hibernate,
			forceCritical: false,
			disableWakeEvent: false
		);

		//int errorCode = Marshal.GetLastWin32Error();

		return success;
	}

	[DllImport("powrprof.dll", SetLastError = true)]
	[return: MarshalAs(UnmanagedType.Bool)]
	static extern bool SetSuspendState(
		 bool hibernate,
		 bool forceCritical,
		 bool disableWakeEvent
	);
}
