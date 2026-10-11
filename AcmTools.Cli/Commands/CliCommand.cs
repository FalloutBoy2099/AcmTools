namespace AcmTools.Cli.Commands
{
	using System;
	using System.Collections.Generic;
	using AcmTools.Acm;
	using AcmTools.Conversion;
	using Localization;

	internal abstract class CliCommand(String name, String arguments, Int32 positionalCount, IReadOnlyCollection<String> flags, IReadOnlyCollection<String> valueOptions)
	{
		protected const String TrustHeaderChannelsFlag = "--trust-header-channels";

		public String Name { get; } = name;

		public String Usage { get; } = $"{name} {arguments}";

		public async Task<Int32> RunAsync(IReadOnlyList<String> args)
		{
			try
			{
				CommandLine commandLine = CommandLine.Parse(args, flags, valueOptions);
				if (commandLine.HelpRequested)
				{
					ConsoleReport.WriteUsage(Usage);
					return 0;
				}

				if (commandLine.Positionals.Count != positionalCount)
				{
					throw new CliUsageException(AppText.Format("ExpectedArguments", positionalCount, commandLine.Positionals.Count));
				}

				return await ExecuteAsync(commandLine).ConfigureAwait(false);
			}
			catch (CliUsageException ex)
			{
				ConsoleReport.WriteError(ex.Message);
				ConsoleReport.WriteUsage(Usage);
				return 1;
			}
		}

		protected abstract Task<Int32> ExecuteAsync(CommandLine commandLine);

		protected static AcmDecodeOptions ReadDecodeOptions(CommandLine commandLine)
		{
			return new AcmDecodeOptions
			{
				ForceFalloutMono = !commandLine.HasFlag(TrustHeaderChannelsFlag)
			};
		}

		protected static String FormatList()
		{
			return String.Join('|', AudioFormats.Exportable.Select(format => format.ToString().ToLowerInvariant()));
		}
	}
}