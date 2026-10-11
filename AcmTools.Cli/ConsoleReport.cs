namespace AcmTools.Cli
{
	using System;
	using System.Globalization;
	using AcmTools.Acm;
	using AcmTools.Cli.Localization;
	using AcmTools.Conversion;
	using Spectre.Console;

	internal static class ConsoleReport
	{
		public static void WriteUsage(String usage)
		{
			AnsiConsole.MarkupLine($"  [cyan]acmconverter[/] {Markup.Escape(usage)}");
		}

		public static void WriteError(String message)
		{
			AnsiConsole.MarkupLine($"[red]{Markup.Escape(message)}[/]");
		}

		public static void WriteResult(ConversionResult result)
		{
			AnsiConsole.MarkupLine($"[green]OK[/] {Markup.Escape(result.OutputPath)}");
			foreach (String step in result.Steps)
			{
				AnsiConsole.MarkupLine($"  - {Markup.Escape(AppText.LocalizeStep(step))}");
			}
		}

		public static void WriteInspection(AcmInfo info)
		{
			Table table = new();
			table.AddColumn(AppText.Get("FieldColumn"));
			table.AddColumn(AppText.Get("ValueColumn"));
			AddRow(table, AppText.Get("Samples"), info.SampleCount);
			AddRow(table, AppText.Get("HeaderChannels"), info.HeaderChannels);
			AddRow(table, AppText.Get("EffectiveChannels"), info.Channels);
			AddRow(table, AppText.Get("SampleRate"), info.SampleRate);
			AddRow(table, AppText.Get("AcmLevel"), info.Level);
			AddRow(table, AppText.Get("Rows"), info.Rows);
			AnsiConsole.Write(table);
		}

		private static void AddRow(Table table, String label, IFormattable value)
		{
			table.AddRow(label, value.ToString(null, CultureInfo.InvariantCulture));
		}
	}
}