namespace AcmTools.Cli.Interactive
{
	using System;
	using System.IO;
	using System.Threading;
	using Spectre.Console;
	using AcmTools.Acm;
	using AcmTools.Conversion;
	using Localization;

	internal sealed class InteractiveMenu(AcmConverter converter)
	{
		private enum MenuAction
		{
			Inspect,
			ToAudio,
			ToAcm,
			Language,
			Quit
		}

		public async Task<Int32> RunAsync()
		{
			AnsiConsole.MarkupLine($"[grey]{Markup.Escape(AppText.Get("ToolkitTagline"))}[/]");
			AnsiConsole.WriteLine();

			while (true)
			{
				MenuAction action = AnsiConsole.Prompt(
					new SelectionPrompt<MenuAction>()
						.Title(AppText.Get("MainMenuTitle"))
						.PageSize(8)
						.UseConverter(Describe)
						.AddChoices(Enum.GetValues<MenuAction>()));

				if (action == MenuAction.Quit)
				{
					return 0;
				}

				AnsiConsole.WriteLine();
				try
				{
					switch (action)
					{
						case MenuAction.Inspect:
							Inspect();
							break;
						case MenuAction.ToAudio:
							await ToAudioAsync().ConfigureAwait(false);
							break;
						case MenuAction.ToAcm:
							await ToAcmAsync().ConfigureAwait(false);
							break;
						case MenuAction.Language:
							ChangeLanguage();
							break;
					}
				}
				catch (OperationCanceledException)
				{
					ConsoleReport.WriteError(AppText.Get("Cancelled"));
				}
				catch (Exception ex)
				{
					ConsoleReport.WriteError(ex.Message);
				}

				AnsiConsole.WriteLine();
			}
		}

		private void Inspect()
		{
			String inputPath = Prompts.ExistingPath(AppText.Get("SourceAcm"));
			AcmDecodeOptions options = new()
			{
				Channels = Prompts.Channels()
			};
			ConsoleReport.WriteInspection(converter.Inspect(inputPath, options));
		}

		private async Task ToAudioAsync()
		{
			String inputPath = Prompts.ExistingPath(AppText.Get("SourceAcm"));
			String outputPath = Prompts.OutputPath(AppText.Get("TargetAudio"), Path.ChangeExtension(inputPath, ".wav"));
			AudioFormat format = PromptOutputFormat(outputPath);
			AcmDecodeOptions options = new()
			{
				Channels = Prompts.Channels(),
				Overwrite = Prompts.YesNo(AppText.Get("OverwriteConfirm"), true)
			};

			ConversionResult result = await RunWithStatusAsync(
				AppText.Get("ToAudioStatus"),
				token => converter.ConvertAcmToAudioAsync(inputPath, outputPath, format, options, token)).ConfigureAwait(false);
			ConsoleReport.WriteResult(result);
		}

		private async Task ToAcmAsync()
		{
			AnsiConsole.Write(new Panel(new Markup(
				$"[cyan]{Markup.Escape(AppText.Get("ToAcmIntro"))}[/]"))
				.Header(AppText.Get("ToAcmHeader"))
				.BorderColor(Color.Cyan1));

			String inputPath = Prompts.ExistingPath(AppText.Get("SourceAudio"));
			String outputPath = Prompts.OutputPath(AppText.Get("TargetAcm"), Path.ChangeExtension(inputPath, ".acm"));
			Int32 sampleRate = Prompts.Integer(AppText.Get("SampleRatePrompt"), AcmEncodeOptions.DefaultSampleRate, AcmEncodeOptions.MinSampleRate, AcmEncodeOptions.MaxSampleRate);
			Boolean compressed = Prompts.YesNo(AppText.Get("TransformConfirm"), true);
			AcmEncodeOptions options = new()
			{
				SampleRate = sampleRate,
				TransformLevel = compressed ? AcmEncodeOptions.DefaultTransformLevel : 0,
				TargetSnrDb = compressed ? Prompts.Integer(AppText.Get("SnrPrompt"), (Int32)AcmEncodeOptions.DefaultTargetSnrDb, 10, 90) : AcmEncodeOptions.DefaultTargetSnrDb,
				MaxBitsPerSample = compressed
					? AcmEncodeOptions.LosslessBitsPerSample
					: Prompts.Integer(AppText.Get("MaxBitsPrompt"), AcmEncodeOptions.LosslessBitsPerSample, AcmEncodeOptions.MinBitsPerSample, AcmEncodeOptions.LosslessBitsPerSample),
				Channels = Prompts.Channels(),
				Overwrite = Prompts.YesNo(AppText.Get("OverwriteConfirm"), true)
			};

			ConversionResult result = await RunWithStatusAsync(
				AppText.Get("ToAcmStatus"),
				token => converter.ConvertAudioToAcmAsync(inputPath, outputPath, options, token)).ConfigureAwait(false);
			ConsoleReport.WriteResult(result);
		}

		private static void ChangeLanguage()
		{
			LanguageOption language = AnsiConsole.Prompt(
				new SelectionPrompt<LanguageOption>()
					.Title(AppText.Get("LanguageTitle"))
					.PageSize(12)
					.UseConverter(option => option.DisplayName)
					.AddChoices(AppText.Languages));
			AppText.SetLanguage(language.Language);
			AnsiConsole.MarkupLine($"[green]{Markup.Escape(AppText.Format("LanguageChanged", language.DisplayName))}[/]");
		}

		private static AudioFormat PromptOutputFormat(String outputPath)
		{
			AudioFormat automatic = AudioFormats.FromPath(outputPath) is { } inferred && inferred != AudioFormat.Acm
				? inferred
				: AudioFormat.Wav;

			AudioFormat choice = AnsiConsole.Prompt(
				new SelectionPrompt<AudioFormat>()
					.Title(AppText.Get("FormatTitle"))
					.UseConverter(format => format == AudioFormat.Auto ? AppText.Format("AutoFormat", automatic) : format.ToString())
					.AddChoices([AudioFormat.Auto, .. AudioFormats.Exportable]));

			return choice == AudioFormat.Auto ? automatic : choice;
		}

		private static async Task<ConversionResult> RunWithStatusAsync(String label, Func<CancellationToken, Task<ConversionResult>> action)
		{
			using ConsoleCancellation cancellation = new();
			return await AnsiConsole.Status()
				.Spinner(Spinner.Known.Dots)
				.SpinnerStyle(Style.Parse("cyan"))
				.StartAsync(label, _ => action(cancellation.Token)).ConfigureAwait(false);
		}

		private static String Describe(MenuAction action)
		{
			return action switch
			{
				MenuAction.Inspect => AppText.Get("InspectAction"),
				MenuAction.ToAudio => AppText.Get("ToAudioAction"),
				MenuAction.ToAcm => AppText.Get("ToAcmAction"),
				MenuAction.Language => AppText.Get("LanguageAction"),
				MenuAction.Quit => AppText.Get("QuitAction"),
				_ => action.ToString()
			};
		}
	}
}