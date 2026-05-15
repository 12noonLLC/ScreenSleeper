using System.Runtime.InteropServices;

namespace Shared;

public static class ScreenState
{
	public static void SetScreenOn() => SetScreenState(State.On);
	public static void SetScreenStandby() => SetScreenState(State.StandBy);
	public static void SetScreenOff() => SetScreenState(State.Off);

	private static void SetScreenState(State state)
	{
		// https://docs.microsoft.com/en-us/windows/win32/menurc/wm-syscommand
		_ = PostMessage(Constants.HWND_BROADCAST, Messages.WM_SYSCOMMAND, Constants.SC_MONITORPOWER, (int)state);
	}


	private static class Messages
	{
		public const int WM_SYSCOMMAND = 0x0112;
	}
	private static class Constants
	{
		public static readonly nint HWND_BROADCAST = 0xFFFF;
		public const int SC_MONITORPOWER = 0xF170;
	}

	private enum State
	{
		On = -1,
		StandBy = 1,
		Off = 2,
	}

	[DllImport("user32.dll")]
	[return: MarshalAs(UnmanagedType.Bool)]
	private static extern bool PostMessage(nint hWnd, int msg, int wParam, int lParam);
}
