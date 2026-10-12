namespace AcmTools.Cli.Commands
{
	using System;
	using System.Collections.Generic;
	using System.Threading;
	using AcmTools.Acm;
	using AcmTools.Conversion;

	internal sealed class ToAcmCommand(AcmConverter converter)
		: CliCommand(
			"to-acm",
			$"<input.{FormatList()}|folder|pattern> <output.acm|folder> [{RateOption} {AcmEncodeOptions.DefaultSampleRate}] [{LevelOption} {AcmEncodeOptions.DefaultTransformLevel}] [{SnrOption} {(Int32)AcmEncodeOptions.DefaultTargetSnrDb}] [{MaxBitsOption} {AcmEncodeOptions.LosslessBitsPerSample}] [{RecursiveFlag}] [{JobsOption} N] [{NoOverwriteFlag}] [{ChannelsOption} auto|mono|stereo]",
			2,
			[NoOverwriteFlag, RecursiveFlag],
			[RateOption, LevelOption, SnrOption, MaxBitsOption, JobsOption, ChannelsOption])
	{
		private const String RateOption = "--rate";
		private const String MaxBitsOption = "--max-bits";
		private const String LevelOption = "--level";
		private const String SnrOption = "--snr";

		private const Int32 MinSnrDb = 10;
		private const Int32 MaxSnrDb = 90;

		protected override async Task<Int32> ExecuteAsync(CommandLine commandLine, CancellationToken cancellationToken)
		{
			String input = commandLine.Positionals[0];
			String output = commandLine.Positionals[1];
			AcmEncodeOptions options = new()
			{
				SampleRate = commandLine.GetInt32(RateOption, AcmEncodeOptions.DefaultSampleRate, AcmEncodeOptions.MinSampleRate, AcmEncodeOptions.MaxSampleRate),
				TransformLevel = commandLine.GetInt32(LevelOption, AcmEncodeOptions.DefaultTransformLevel, 0, AcmEncodeOptions.MaxTransformLevel),
				TargetSnrDb = commandLine.GetInt32(SnrOption, (Int32)AcmEncodeOptions.DefaultTargetSnrDb, MinSnrDb, MaxSnrDb),
				MaxBitsPerSample = commandLine.GetInt32(MaxBitsOption, AcmEncodeOptions.LosslessBitsPerSample, AcmEncodeOptions.MinBitsPerSample, AcmEncodeOptions.LosslessBitsPerSample),
				Channels = ReadChannels(commandLine),
				Overwrite = !commandLine.HasFlag(NoOverwriteFlag)
			};

			if (BatchPlanner.IsBatchInput(input))
			{
				IReadOnlyList<BatchItem> items = BatchPlanner.Plan(input, output, AudioFormats.SourceExtensions, AudioFormats.DefaultExtension(AudioFormat.Acm), commandLine.HasFlag(RecursiveFlag));
				return await BatchRunner.RunAsync(
					items,
					(item, token) => converter.ConvertAudioToAcmAsync(item.Input, item.Output, options, token),
					options.Overwrite,
					ReadJobs(commandLine),
					cancellationToken).ConfigureAwait(false);
			}

			ConsoleReport.WriteResult(await converter.ConvertAudioToAcmAsync(input, output, options, cancellationToken).ConfigureAwait(false));
			return 0;
		}
	}
}