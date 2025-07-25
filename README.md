# ScreenSleeper by [12noon LLC](https://12noon.com)

[![Build and Tests](https://github.com/12noonLLC/ScreenSleeper/actions/workflows/build.yml/badge.svg)](https://github.com/12noonLLC/ScreenSleeper/actions/workflows/build.yml)

Turn off screen after user idle or delay.

## Features

Windows sometimes forgets to turn off your screens according
to your *Power & sleep* settings.
This application ensures your monitors are turned off on demand,
after user idle, or after a simple delay.

When you run this application, you can specify the delay before
it turns off the monitors. You can also specify that it sets
the monitors to standby first and waits an additional period of time
before turning off the monitors.

The application can also lock the computer when it turns off the monitors.

## Steps

These steps are how the application uses the values specified on the command line.

1. If `/idle` value is greater than zero, wait until computer has been idle this long.
2. If `/delay` value is greater than zero, wait this long.
3. If `/standby` value is greater than zero, set monitor(s) to standby and wait this long before turning them off.
4. If `/lock` is specified, lock Windows.
5. Turn off monitor(s).

*ScreenSleeper* always waits at least one second before turning off the monitors
so that the user has time to stop using the keyboard and mouse.
(Otherwise, they might unintentionally wake up the computer.)

## Example Tasks

### Turn off monitors immediately

`ScreenSleeper.exe`

### Wait five seconds and then turn off monitors

`ScreenSleeper.exe /delay 5`

### Wait five seconds and then turn off monitors and lock Windows

`ScreenSleeper.exe /delay 5 /lock`

### When user has been idle for ten minutes, turn off monitors and lock Windows

`ScreenSleeper.exe /idle 10 /lock`

### When user has been idle for ten minutes, wait three more seconds and then turn off monitors and lock Windows

`ScreenSleeper.exe /idle 10 /delay 3 /lock`

### Wait five seconds; set monitors to standby for ten seconds; and then turn them off

`ScreenSleeper.exe /delay 5 /standby 10`


## Command Line

Key            | Action
:------------- | :-----
/? | Display this information
/h | Display this information
/idle \<minutes\> | Wait until the user has been idle for this long
/delay \<seconds\> | Wait before entering standby (if /standby is specified) or off (Default = 1)
/standby \<seconds\> | Wait in standby before turning off monitors
/lock | Lock the computer after turning off monitors

## Reference

https://docs.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-registerpowersettingnotification

https://docs.microsoft.com/en-us/windows/win32/power/wm-powerbroadcast

**WM_POWERBROADCAST**
https://docs.microsoft.com/en-us/windows/win32/api/winuser/ns-winuser-powerbroadcast_setting

**POWERBROADCAST_SETTING**
https://docs.microsoft.com/en-us/windows/win32/power/power-setting-guids
**GUID_CONSOLE_DISPLAY_STATE**

https://www.codeproject.com/Articles/1193099/Determining-the-Monitors-On-Off-sleep-Status

https://stackoverflow.com/questions/203355/is-there-any-way-to-detect-the-monitor-state-in-windows-on-or-off

https://stackoverflow.com/questions/31911432/c-sharp-wpf-application-compiling-wm-powerbroadcast
https://stackoverflow.com/questions/3355606/detect-laptop-lid-closure-and-opening/23327280#23327280
