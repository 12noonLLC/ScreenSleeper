using Shared;
using System;
using System.CommandLine;
using System.CommandLine.Help;
using System.CommandLine.Invocation;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

/*
	FEATURES

	When run from command line, delay before screen-off.
	Delay between standby and off.
	Wait for user-idle, delay, then standby/off.

	Command Line Arguments
		--help
		--idle 10
		--delay 5
		--standby 10 [default 0 => no standby]
 */
namespace ScreenSleeper;

internal class CustomHelpAction(HelpAction _defaultAction) : SynchronousCommandLineAction
{
	public override int Invoke(ParseResult parseResult)
	{
		parseResult.InvocationConfiguration.Output.Write("header");

		int result = _defaultAction.Invoke(parseResult);

		//parseResult.InvocationConfiguration.Output.WriteLine("FOOTER");

		return result;

	}
}

public class Program
{
	private static TimeSpan DelayTimeDefault = TimeSpan.FromSeconds(3);

	static void Main(string[] args)
	{
		ConsoleWindow.InstallLazyConsole();

#if DEBUG
		//args = [ "--idle", "0.1", "--delay", "3", "--standby", "5", "--lock", ];
		//args = [ "--help", "--delay", "10", "--idle", "1", ];
#endif

		if (!Application.IsFirst)
		{
			return;
		}

		Option<TimeSpan> idleOption = new("--idle")
		{
			Description = "Wait until idle for this long (minutes)",
			CustomParser = parseResult =>
			{
				if (!parseResult.Tokens.Any())
				{
					return TimeSpan.Zero;
				}

				if (!int.TryParse(parseResult.Tokens.Single().Value, out int value))
				{
					parseResult.AddError("The value must be an integer.");
					return TimeSpan.Zero; // Ignored.
				}

				TimeSpan result = TimeSpan.FromMinutes(value);

				if (result < TimeSpan.Zero)
				{
					parseResult.AddError($"{parseResult.Argument.Name} must be greater than zero.");
				}
				return result;
			},
		};

		Option<TimeSpan> delayOption = new("--delay")
		{
			Description = "Wait before entering standby (if --standby specified) or off (seconds)",
			DefaultValueFactory = ParseResult => DelayTimeDefault,
			CustomParser = parseResult =>
			{
				if (!parseResult.Tokens.Any())
				{
					return DelayTimeDefault;
				}

				if (!int.TryParse(parseResult.Tokens.Single().Value, out int value))
				{
					parseResult.AddError("The value must be an integer.");
					return DelayTimeDefault; // Ignored.
				}

				TimeSpan result = TimeSpan.FromSeconds(value);

				if (result < DelayTimeDefault)
				{
					parseResult.AddError($"{parseResult.Argument.Name} must be {DelayTimeDefault.TotalSeconds} seconds or more.");
				}
				return result;
			},
		};

		Option<TimeSpan> standbyOption = new("--standby")
		{
			Description = "Wait before turning off monitors (seconds)",
			CustomParser = parseResult =>
			{
				if (!parseResult.Tokens.Any())
				{
					return TimeSpan.Zero;
				}

				if (!int.TryParse(parseResult.Tokens.Single().Value, out int value))
				{
					parseResult.AddError("The value must be an integer.");
					return TimeSpan.Zero; // Ignored.
				}

				TimeSpan result = TimeSpan.FromSeconds(value);

				if (result < TimeSpan.Zero)
				{
					parseResult.AddError($"{parseResult.Argument.Name} must be greater than zero.");
				}
				return result;
			},
		};

		Option<bool> lockOption = new("--lock")
		{
			Description = "Lock the computer after turning off monitors",
		};

		Option<bool> sleepOption = new("--sleep")
		{
			Description = "Put the computer to sleep after turning off monitors",
		};

		Option<bool> hibernateOption = new("--hibernate")
		{
			Description = "Hibernate the computer after turning off monitors",
		};

		RootCommand rootCommand = new("ScreenSleeper - A utility to turn off the monitor with optional delays and user idle time.")
		{
			TreatUnmatchedTokensAsErrors = true,
		};

		// HelpOption and VersionOption are added automatically.
		rootCommand.Options.Add(idleOption);
		rootCommand.Options.Add(delayOption);
		rootCommand.Options.Add(standbyOption);
		rootCommand.Options.Add(lockOption);
		rootCommand.Options.Add(sleepOption);
		rootCommand.Options.Add(hibernateOption);

		rootCommand.SetAction(parseResult =>
		{
			TimeSpan delay = parseResult.GetValue(delayOption);
			ProcessScreenStates(
				parseResult.GetValue(idleOption),
				(delay == TimeSpan.Zero) ? DelayTimeDefault : delay,
				parseResult.GetValue(standbyOption),
				parseResult.GetValue(lockOption),
				parseResult.GetValue(sleepOption),
				parseResult.GetValue(hibernateOption)
			).Wait();
		});

		ParseResult parseResult = rootCommand.Parse(args);
		if (parseResult.Errors.Count > 0)
		{
			parseResult.InvocationConfiguration.Output.WriteLine();
			foreach (var error in parseResult.Errors)
			{
				parseResult.InvocationConfiguration.Output.WriteLine(error.Message);
			}
			parseResult.InvocationConfiguration.Output.WriteLine();
			parseResult.InvocationConfiguration.Output.WriteLine("Use --help for usage information.");
			parseResult.InvocationConfiguration.Output.WriteLine();

			Task.Delay(TimeSpan.FromSeconds(5)).Wait();
			return;
		}

		// If the user requested help, print application information before the help text.
		if (parseResult.Action is HelpAction helpAction)
		{
			ApplicationInformation appInfo = new();
			TextWriter output = parseResult.InvocationConfiguration.Output;
			output.WriteLine();
			output.WriteLine($"{appInfo.Name} {appInfo.VersionShort}");
			output.WriteLine(appInfo.Company);
			output.WriteLine(appInfo.Copyright);
			output.WriteLine();
			new CustomHelpAction(helpAction).Invoke(parseResult);
			return;
		}

		parseResult.Invoke();
	}

	private static async Task ProcessScreenStates(TimeSpan delayIdle, TimeSpan delay, TimeSpan delayStandby, bool bLock, bool bSleep, bool bHibernate)
	{
		/// If user idle time is specified, wait for it.
		await WaitForUserIdle(delayIdle);

		if (delay != TimeSpan.Zero)
		{
			await Task.Delay(delay);
		}

		/// If a standby period is specified, set to standby and wait.
		if (delayStandby != TimeSpan.Zero)
		{
			ScreenState.SetScreenStandby();
			await Task.Delay(delayStandby);
		}

		// Note that we must lock before turning off the monitor because locking wakes it up.
		if (bLock)
		{
			LockWindows.Lock();
		}

		/// Finally, hibernate, sleep, or power off--in order of decreasing power-saving.
		if (bHibernate)
		{
			PowerState.Hibernate();
		}
		else if (bSleep)
		{
			PowerState.Sleep();
		}
		else
		{
			ScreenState.SetScreenOff();
		}
	}

	private static async Task WaitForUserIdle(TimeSpan delayIdle)
	{
		if (delayIdle == TimeSpan.Zero)
		{
			return;
		}

		while (true)
		{
			var remainingTime = delayIdle.Subtract(UserIdle.GetTimeSinceLastActivity());
			if (remainingTime > TimeSpan.Zero)
			{
				await Task.Delay(remainingTime);
			}
			else
			{
				break;
			}
		}
	}
}
