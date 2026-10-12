namespace AcmTools.Cli.Commands
{
	using System;
	using System.Collections.Generic;
	using System.Threading;
	using AcmTools.Acm;
	using AcmTools.Conversion;

	internal sealed class ToAudioCommand(AcmConverter converter)
		: CliCommand(
			"to-audio",
			$"<input.acm|folder|pattern> <output.{FormatList()}|folder> [{FormatOption} {FormatList()}] [{RecursiveFlag}] [{JobsOption} N] [{NoOverwriteFlag}] [{ChannelsOption} auto|mono|stereo]",
			2,
			[TrustHeaderChannelsFlag, NoOverwriteFlag, RecursiveFlag],
			[FormatOption, JobsOption, ChannelsOption])
	{
		protected override async Task<Int32> ExecuteAsync(CommandLine commandLine, CancellationToken cancellationToken)
		{
			String input = commandLine.Positionals[0];
			String output = commandLine.Positionals[1];
			AcmDecodeOptions options = ReadDecodeOptions(commandLine);
			AudioFormat format = ReadFormat(commandLine);

			if (BatchPlanner.IsBatchInput(input))
			{
				AudioFormat target = format == AudioFormat.Auto ? AudioFormat.Wav : format;
				IReadOnlyList<BatchItem> items = BatchPlanner.Plan(input, output, [".acm"], AudioFormats.DefaultExtension(target), commandLine.HasFlag(RecursiveFlag));
				return await BatchRunner.RunAsync(
					items,
					(item, token) => converter.ConvertAcmToAudioAsync(item.Input, item.Output, target, options, token),
					options.Overwrite,
					ReadJobs(commandLine),
					cancellationToken).ConfigureAwait(false);
			}

			ConsoleReport.WriteResult(await converter.ConvertAcmToAudioAsync(input, output, format, options, cancellationToken).ConfigureAwait(false));
			return 0;
		}
	}
}