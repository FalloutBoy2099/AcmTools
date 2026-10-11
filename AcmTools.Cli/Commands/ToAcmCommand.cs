namespace AcmTools.Cli.Commands
{
	using System;
	using AcmTools.Acm;
	using AcmTools.Conversion;

	internal sealed class ToAcmCommand(AcmConverter converter)
		: CliCommand("to-acm", $"<input.{FormatList()}> <output.acm> [{RateOption} {AcmEncodeOptions.DefaultSampleRate}] [{NoOverwriteFlag}]", 2, [NoOverwriteFlag], [RateOption])
	{
		private const String RateOption = "--rate";
		private const String NoOverwriteFlag = "--no-overwrite";

		protected override async Task<Int32> ExecuteAsync(CommandLine commandLine)
		{
			AcmEncodeOptions options = new()
			{
				SampleRate = commandLine.GetInt32(RateOption, AcmEncodeOptions.DefaultSampleRate, AcmEncodeOptions.MinSampleRate, AcmEncodeOptions.MaxSampleRate),
				ForceMono = true,
				Overwrite = !commandLine.HasFlag(NoOverwriteFlag)
			};
			ConversionResult result = await converter
				.ConvertAudioToAcmAsync(commandLine.Positionals[0], commandLine.Positionals[1], options)
				.ConfigureAwait(false);
			ConsoleReport.WriteResult(result);
			return 0;
		}
	}
}
