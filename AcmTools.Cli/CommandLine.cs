namespace AcmTools.Cli
{
	using System;
	using System.Collections.Generic;
	using System.Globalization;

	/// <summary>Arguments of a single command, split into positional arguments, flags and valued options.</summary>
	internal sealed class CommandLine
	{
		private static readonly String[] HelpTokens = ["/?", "-?", "-h", "--help"];

		private readonly HashSet<String> flags = new(StringComparer.OrdinalIgnoreCase);
		private readonly Dictionary<String, String> values = new(StringComparer.OrdinalIgnoreCase);
		private readonly List<String> positionals = [];

		private CommandLine()
		{
		}

		public IReadOnlyList<String> Positionals => positionals;

		public Boolean HelpRequested { get; private set; }

		public static Boolean IsHelpToken(String value)
		{
			return String.Equals(value, "help", StringComparison.OrdinalIgnoreCase) || IsHelpSwitch(value);
		}

		public static CommandLine Parse(IReadOnlyList<String> args, IReadOnlyCollection<String> knownFlags, IReadOnlyCollection<String> knownValueOptions)
		{
			CommandLine commandLine = new();
			for (Int32 i = 0; i < args.Count; i++)
			{
				String arg = args[i];
				if (IsHelpSwitch(arg) || (i == 0 && IsHelpToken(arg)))
				{
					commandLine.HelpRequested = true;
				}
				else if (!arg.StartsWith("--", StringComparison.Ordinal))
				{
					commandLine.positionals.Add(arg);
				}
				else if (Contains(knownFlags, arg))
				{
					commandLine.flags.Add(arg);
				}
				else if (Contains(knownValueOptions, arg))
				{
					i++;
					commandLine.values[arg] = i < args.Count
						? args[i]
						: throw new CliUsageException($"Missing value for option {arg}.");
				}
				else
				{
					throw new CliUsageException($"Unknown option: {arg}");
				}
			}

			return commandLine;
		}

		public Boolean HasFlag(String name)
		{
			return flags.Contains(name);
		}

		public Int32 GetInt32(String name, Int32 defaultValue, Int32 min, Int32 max)
		{
			if (!values.TryGetValue(name, out String? text))
			{
				return defaultValue;
			}

			if (!Int32.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out Int32 value) || value < min || value > max)
			{
				throw new CliUsageException(
					$"Option {name} expects an integer between {min.ToString(CultureInfo.InvariantCulture)} and {max.ToString(CultureInfo.InvariantCulture)}, got '{text}'.");
			}

			return value;
		}

		private static Boolean IsHelpSwitch(String value)
		{
			return Array.Exists(HelpTokens, token => String.Equals(token, value, StringComparison.OrdinalIgnoreCase));
		}

		private static Boolean Contains(IReadOnlyCollection<String> names, String arg)
		{
			return names.Contains(arg, StringComparer.OrdinalIgnoreCase);
		}
	}
}