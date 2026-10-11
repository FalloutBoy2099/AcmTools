namespace AcmTools.Cli
{
	using System;
	using System.Globalization;
	using AcmTools.Acm;
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
				AnsiConsole.MarkupLine($"  - {Markup.Escape(step)}");
			}
		}

		public static void WriteInspection(AcmInfo info)
		{
			Table table = new();
			table.AddColumn("Field");
			table.AddColumn("Value");
			AddRow(table, "Samples", info.SampleCount);
			AddRow(table, "Header channels", info.HeaderChannels);
			AddRow(table, "Effective channels", info.Channels);
			AddRow(table, "Sample rate", info.SampleRate);
			AddRow(table, "ACM level", info.Level);
			AddRow(table, "Rows", info.Rows);
			AnsiConsole.Write(table);
		}

		private static void AddRow(Table table, String label, IFormattable value)
		{
			table.AddRow(label, value.ToString(null, CultureInfo.InvariantCulture));
		}
	}
}