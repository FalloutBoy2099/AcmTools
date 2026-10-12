namespace AcmTools.Cli.Commands
{
	using System;
	using System.Collections.Generic;
	using System.Threading;
	using AcmTools.Acm;
	using AcmTools.Conversion;
	using Localization;

	internal abstract class CliCommand(String name, String arguments, Int32 positionalCount, IReadOnlyCollection<String> flags, IReadOnlyCollection<String> valueOptions)
	{
		protected const String TrustHeaderChannelsFlag = "--trust-header-channels";
		protected const String ChannelsOption = "--channels";
		protected const String NoOverwriteFlag = "--no-overwrite";
		protected const String RecursiveFlag = "--recursive";
		protected const String JobsOption = "--jobs";
		protected const String FormatOption = "--format";

		private const Int32 MaxJobs = 64;

		public String Name { get; } = name;

		public String Usage { get; } = $"{name} {arguments}";

		public async Task<Int32> RunAsync(IReadOnlyList<String> args, CancellationToken cancellationToken)
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

				return await ExecuteAsync(commandLine, cancellationToken).ConfigureAwait(false);
			}
			catch (CliUsageException ex)
			{
				ConsoleReport.WriteError(ex.Message);
				ConsoleReport.WriteUsage(Usage);
				return 1;
			}
		}

		protected abstract Task<Int32> ExecuteAsync(CommandLine commandLine, CancellationToken cancellationToken);

		protected static AcmDecodeOptions ReadDecodeOptions(CommandLine commandLine)
		{
			return new AcmDecodeOptions
			{
				Channels = commandLine.HasFlag(TrustHeaderChannelsFlag) ? AcmChannelMode.Stereo : ReadChannels(commandLine),
				Overwrite = !commandLine.HasFlag(NoOverwriteFlag)
			};
		}

		/// <summary>Reads <c>--channels auto|mono|stereo</c> (automatic when absent).</summary>
		protected static AcmChannelMode ReadChannels(CommandLine commandLine)
		{
			String? text = commandLine.GetString(ChannelsOption);
			if (text is null)
			{
				return AcmChannelMode.Auto;
			}

			if (!Enum.TryParse(text, true, out AcmChannelMode mode) || !Enum.IsDefined(mode))
			{
				throw new CliUsageException(AppText.Format("UnknownChannels", text, "auto, mono, stereo"));
			}

			return mode;
		}

		protected static Int32 ReadJobs(CommandLine commandLine)
		{
			return commandLine.GetInt32(JobsOption, Math.Min(Environment.ProcessorCount, 8), 1, MaxJobs);
		}

		/// <summary>Reads <c>--format</c>; <see cref="AudioFormat.Auto"/> when the option is absent.</summary>
		protected static AudioFormat ReadFormat(CommandLine commandLine)
		{
			String? text = commandLine.GetString(FormatOption);
			if (text is null)
			{
				return AudioFormat.Auto;
			}

			if (!AudioFormats.TryParse(text, out AudioFormat format) || !AudioFormats.Exportable.Contains(format))
			{
				throw new CliUsageException(AppText.Format("UnknownFormat", text, FormatList(", ")));
			}

			return format;
		}

		protected static String FormatList(String separator = "|")
		{
			return String.Join(separator, AudioFormats.Exportable.Select(format => format.ToString().ToLowerInvariant()));
		}
	}
}