using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace Shared;

/// <summary>
/// Wraps <see cref="Console.Out"/> so that a console is only created or attached on the first
/// actual write. After the first write, <see cref="ConsoleWindow.EnsureConsole"/> replaces
/// <see cref="Console.Out"/> with a direct stream writer, so subsequent writes bypass this
/// class entirely.
/// </summary>
/// <remarks>
/// Install once at the very start of <c>Main</c> before any output can occur:
/// <code>
/// ConsoleWindow.InstallLazyConsole();   // Console.Out is now a LazyConsoleWriter
///
/// // ... parse args, invoke commands, etc. ...
///
/// // First Console.Write/WriteLine call triggers EnsureConsole(), which:
/// //   1. Attaches to the parent process console (if launched from cmd/PowerShell), or
/// //   2. Creates a new console window (if launched from Explorer / WIN+R).
/// // Console.Out is then replaced with a direct StreamWriter; this class is no longer called.
/// Console.WriteLine("This triggers console creation if needed.");
/// </code>
/// If the app runs silently with no output, no console window is ever created.
/// </remarks>
internal sealed class LazyConsoleWriter : TextWriter
{
	private bool _initialized;

	public override Encoding Encoding => Encoding.UTF8;

	private void EnsureOnce()
	{
		if (_initialized)
		{
			return;
		}
		_initialized = true;
		ConsoleWindow.EnsureConsole();
	}

	public override void Write(char value)				{ EnsureOnce(); Console.Out.Write(value); }
	public override void Write(string? value)			{ EnsureOnce(); Console.Out.Write(value); }
	public override void WriteLine(string? value)	{ EnsureOnce(); Console.Out.WriteLine(value); }
	public override void WriteLine()						{ EnsureOnce(); Console.Out.WriteLine(); }
}

/// <summary>
/// This class adds a console window to the process.
/// This is primarily useful for a GUI application.
/// </summary>
public static class ConsoleWindow
{
	public static bool HasConsoleWindow() => (GetConsoleWindow() != IntPtr.Zero);
	public static void ShowConsoleWindow() => ShowWindow(GetConsoleWindow(), (int)ShowWindowState.SW_SHOW);
	public static void HideConsoleWindow() => ShowWindow(GetConsoleWindow(), (int)ShowWindowState.SW_HIDE);

	/// <summary>
	/// Installs a lazy <see cref="TextWriter"/> as <see cref="Console.Out"/> so that a console
	/// is only created or attached the first time something is actually written.
	/// </summary>
	public static void InstallLazyConsole()
	{
		Console.SetOut(new LazyConsoleWriter());
	}

	public static bool EnsureConsole()
	{
		// If a console window is already present, use it.
		if (HasConsoleWindow())
		{
			return true;
		}

		// If the parent process has a console window, attach to it.
		if (AttachConsole(ATTACH_PARENT_PROCESS) && HasConsoleWindow())
		{
			InitializeConsoleStreams();
			return true;
		}

		// Otherwise, create a new console window.
		CreateConsoleWindow();

		if (HasConsoleWindow())
		{
			return true;
		}

		return false;
	}

	private static void InitializeConsoleStreams()
	{
		StreamWriter output = new(Console.OpenStandardOutput()) { AutoFlush = true, };
		Console.SetOut(output);

		StreamWriter error = new(Console.OpenStandardError()) { AutoFlush = true, };
		Console.SetError(error);
	}

	/// <summary>
	/// This method creates a console window and redirects standard-output to it.
	/// </summary>
	/// <remarks>
	/// This will not output to the console window if Visual Studio is attached to the process.
	/// </remarks>
	public static void CreateConsoleWindow()
	{
		// Only one console per process.
		if (AllocConsole() == 0)
		{
			return;
		}

		InitializeConsoleStreams();

		// Now, Console.WriteLine() will output to the new console window (unless the Visual Studio debugger is attached).
	}


	[DllImport("user32.dll")]
	private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

	private enum ShowWindowState
	{
		SW_HIDE = 0,
		SW_SHOWMINIMIZED = 2,
		SW_SHOW = 5,
	}

	[DllImport("kernel32.dll")]
	private static extern IntPtr GetConsoleWindow();

	[DllImport("kernel32.dll", EntryPoint = "AllocConsole", SetLastError = true, CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
	private static extern int AllocConsole();

	private const uint ATTACH_PARENT_PROCESS = 0xFFFFFFFF;

	[DllImport("kernel32.dll")]
	private static extern bool AttachConsole(uint dwProcessId);
}
