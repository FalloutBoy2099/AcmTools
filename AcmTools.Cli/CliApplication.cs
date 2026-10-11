namespace AcmTools.Cli
{
	using System;
	using System.Collections.Generic;
	using AcmTools.Cli.Commands;
	using AcmTools.Cli.Interactive;
	using AcmTools.Cli.Localization;
	using AcmTools.Conversion;
	using Spectre.Console;

	internal sealed class CliApplication
	{
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

				return await command.RunAsync(args[1..]).ConfigureAwait(false);
			}
			catch (Exception ex)
			{
				ConsoleReport.WriteError(ex.Message);
				return 1;
			}
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
		}
	}
}