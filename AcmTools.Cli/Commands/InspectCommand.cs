namespace AcmTools.Cli.Commands
{
	using System;
	using System.Threading;
	using AcmTools.Conversion;

	internal sealed class InspectCommand(AcmConverter converter)
		: CliCommand("inspect", $"<input.acm> [{ChannelsOption} auto|mono|stereo]", 1, [TrustHeaderChannelsFlag], [ChannelsOption])
	{
		protected override Task<Int32> ExecuteAsync(CommandLine commandLine, CancellationToken cancellationToken)
		{
			ConsoleReport.WriteInspection(converter.Inspect(commandLine.Positionals[0], ReadDecodeOptions(commandLine)));
			return Task.FromResult(0);
		}
	}
}