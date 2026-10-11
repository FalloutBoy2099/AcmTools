namespace AcmTools.Cli.Commands
{
	using System;
	using AcmTools.Conversion;

	internal sealed class InspectCommand(AcmConverter converter)
		: CliCommand("inspect", $"<input.acm> [{TrustHeaderChannelsFlag}]", 1, [TrustHeaderChannelsFlag], [])
	{
		protected override Task<Int32> ExecuteAsync(CommandLine commandLine)
		{
			ConsoleReport.WriteInspection(converter.Inspect(commandLine.Positionals[0], ReadDecodeOptions(commandLine)));
			return Task.FromResult(0);
		}
	}
}