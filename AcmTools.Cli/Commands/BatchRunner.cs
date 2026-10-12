namespace AcmTools.Cli.Commands
{
	using System;
	using System.Collections.Concurrent;
	using System.Collections.Generic;
	using System.IO;
	using System.Threading;
	using AcmTools.Conversion;
	using Localization;
	using Spectre.Console;

	/// <summary>Converts a planned list of files in parallel, reports progress, and keeps going when a file fails.</summary>
	internal static class BatchRunner
	{
		private const Int32 MaxReportedFailures = 20;

		public static async Task<Int32> RunAsync(
			IReadOnlyList<BatchItem> items,
			Func<BatchItem, CancellationToken, Task<ConversionResult>> convert,
			Boolean overwrite,
			Int32 jobs,
			CancellationToken cancellationToken)
		{
			ConcurrentQueue<String> failures = new();
			Int32 converted = 0;
			Int32 skipped = 0;
			Int32 failed = 0;

			Func<ProgressTask?, Task> work = task => Parallel.ForEachAsync(
				items,
				new ParallelOptions { MaxDegreeOfParallelism = jobs, CancellationToken = cancellationToken },
				async (item, token) =>
				{
					if (!overwrite && File.Exists(item.Output))
					{
						Interlocked.Increment(ref skipped);
					}
					else
					{
						try
						{
							await convert(item, token).ConfigureAwait(false);
							Interlocked.Increment(ref converted);
						}
						catch (OperationCanceledException)
						{
							throw;
						}
						catch (Exception ex)
						{
							Interlocked.Increment(ref failed);
							failures.Enqueue(AppText.Format("BatchFailed", item.Input, ex.Message));
						}
					}

					task?.Increment(1);
				});

			String title = Markup.Escape(AppText.Format("BatchConverting", items.Count));
			if (AnsiConsole.Profile.Capabilities.Interactive)
			{
				await AnsiConsole.Progress()
					.AutoClear(true)
					.StartAsync(async context => await work(context.AddTask(title, maxValue: items.Count)).ConfigureAwait(false))
					.ConfigureAwait(false);
			}
			else
			{
				AnsiConsole.MarkupLine($"[grey]{title}[/]");
				await work(null).ConfigureAwait(false);
			}

			Int32 reported = 0;
			foreach (String failure in failures)
			{
				if (reported++ == MaxReportedFailures)
				{
					ConsoleReport.WriteError(AppText.Format("BatchMoreFailures", failed - MaxReportedFailures));
					break;
				}

				ConsoleReport.WriteError(failure);
			}

			String summary = AppText.Format("BatchSummary", converted, skipped, failed);
			AnsiConsole.MarkupLine(failed == 0 ? $"[green]{Markup.Escape(summary)}[/]" : $"[yellow]{Markup.Escape(summary)}[/]");
			return failed == 0 ? 0 : 1;
		}
	}
}