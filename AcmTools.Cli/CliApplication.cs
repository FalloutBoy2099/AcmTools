namespace AcmTools.Cli
{
	using System;
	using System.Collections.Generic;
	using Spectre.Console;
	using AcmTools.Conversion;
	using Commands;
	using Interactive;
	using Localization;

	internal sealed class CliApplication
	{
		private const Int32 CancelledExitCode = 130;
		private const String LanguageOptionName = "--lang";

		private readonly AcmConverter converter = new();
		private readonly IReadOnlyList<CliCommand> commands;

		public CliApplication()
		{
			commands = [new InspectCommand(converter), new ToAudioCommand(converter), new ToAcmCommand(converter)];
		}

		public async Task<Int32> RunAsync(String[] args)
		{
			try
			{
				args = ApplyLanguageOption(args);
				if (args.Length == 0)
				{
					if (IsInteractive())
					{
						return await new InteractiveMenu(converter).RunAsync().ConfigureAwait(false);
					}

					PrintHelp();
					return 0;
				}

				if (CommandLine.IsHelpToken(args[0]))
				{
					PrintHelp();
					return 0;
				}

				CliCommand? command = commands.FirstOrDefault(candidate => String.Equals(candidate.Name, args[0], StringComparison.OrdinalIgnoreCase));
				if (command is null)
				{
					ConsoleReport.WriteError(AppText.Format("UnknownCommand", args[0]));
					return 1;
				}

				using ConsoleCancellation cancellation = new();
				return await command.RunAsync(args[1..], cancellation.Token).ConfigureAwait(false);
			}
			catch (OperationCanceledException)
			{
				ConsoleReport.WriteError(AppText.Get("Cancelled"));
				return CancelledExitCode;
			}
			catch (Exception ex)
			{
				ConsoleReport.WriteError(ex.Message);
				return 1;
			}
		}

		/// <summary>Applies and removes the global <c>--lang code</c> / <c>--lang=code</c> option wherever it appears.</summary>
		private static String[] ApplyLanguageOption(String[] args)
		{
			List<String> remaining = [];
			for (Int32 i = 0; i < args.Length; i++)
			{
				String? code = null;
				if (String.Equals(args[i], LanguageOptionName, StringComparison.OrdinalIgnoreCase))
				{
					code = i + 1 < args.Length
						? args[++i]
						: throw new CliUsageException(AppText.Format("MissingOptionValue", LanguageOptionName));
				}
				else if (args[i].StartsWith(LanguageOptionName + "=", StringComparison.OrdinalIgnoreCase))
				{
					code = args[i][(LanguageOptionName.Length + 1)..];
				}

				if (code is null)
				{
					remaining.Add(args[i]);
				}
				else if (AppText.TryParseLanguage(code, out CliLanguage language))
				{
					AppText.SetLanguage(language);
				}
				else
				{
					throw new CliUsageException(AppText.Format("UnknownLanguage", code, AppText.LanguageCodes));
				}
			}

			return [.. remaining];
		}

		private static Boolean IsInteractive()
		{
			return !Console.IsInputRedirected && !Console.IsOutputRedirected;
		}

		private void PrintHelp()
		{
			AnsiConsole.MarkupLine($"[bold]AcmTools[/] - {Markup.Escape(AppText.Get("ToolkitTagline"))}");
			foreach (CliCommand command in commands)
			{
				ConsoleReport.WriteUsage(command.Usage);
			}

			AnsiConsole.WriteLine();
			AnsiConsole.WriteLine(AppText.Get("HelpFalloutNote"));
			AnsiConsole.WriteLine(AppText.Format("HelpLangOption", AppText.LanguageCodes));
		}
	}
}