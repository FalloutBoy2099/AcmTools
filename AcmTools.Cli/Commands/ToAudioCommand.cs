namespace AcmTools.Cli.Commands
{
	using System;
	using AcmTools.Conversion;

	internal sealed class ToAudioCommand(AcmConverter converter)
		: CliCommand("to-audio", $"<input.acm> <output.{FormatList()}> [{TrustHeaderChannelsFlag}]", 2, [TrustHeaderChannelsFlag], [])
	{
		protected override async Task<Int32> ExecuteAsync(CommandLine commandLine)
		{
			ConversionResult result = await converter
				.ConvertAcmToAudioAsync(commandLine.Positionals[0], commandLine.Positionals[1], AudioFormat.Auto, ReadDecodeOptions(commandLine))
				.ConfigureAwait(false);
			ConsoleReport.WriteResult(result);
			return 0;
		}
	}
}