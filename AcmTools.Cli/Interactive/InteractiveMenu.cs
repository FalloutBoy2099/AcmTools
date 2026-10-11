namespace AcmTools.Cli.Interactive
{
	using System;
	using System.IO;
	using AcmTools.Acm;
	using AcmTools.Conversion;
	using Spectre.Console;

	internal sealed class InteractiveMenu(AcmConverter converter)
	{
		private const String FalloutMonoPrompt = "Mode Fallout mono (recommande pour les ACM Fallout) ?";

		private enum MenuAction
		{
			Inspect,
			ToAudio,
			ToAcm,
			Quit
		}

		public async Task<Int32> RunAsync()
		{
			AnsiConsole.MarkupLine("[grey]Fallout/Interplay ACM audio converter[/]");
			AnsiConsole.WriteLine();

			while (true)
			{
				MenuAction action = AnsiConsole.Prompt(
					new SelectionPrompt<MenuAction>()
						.Title("Que veux-tu faire ?")
						.PageSize(8)
						.UseConverter(Describe)
						.AddChoices(Enum.GetValues<MenuAction>()));

				if (action == MenuAction.Quit)
				{
					return 0;
				}

				AnsiConsole.WriteLine();
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
				}

				AnsiConsole.WriteLine();
			}
		}

		private void Inspect()
		{
			String inputPath = Prompts.ExistingPath("Fichier ACM source");
			AcmDecodeOptions options = new()
			{
				ForceFalloutMono = Prompts.YesNo(FalloutMonoPrompt, true)
			};
			ConsoleReport.WriteInspection(converter.Inspect(inputPath, options));
		}

		private async Task ToAudioAsync()
		{
			String inputPath = Prompts.ExistingPath("Fichier ACM source");
			String outputPath = Prompts.OutputPath("Fichier audio cible", Path.ChangeExtension(inputPath, ".wav"));
			AudioFormat format = PromptOutputFormat(outputPath);
			AcmDecodeOptions options = new()
			{
				ForceFalloutMono = Prompts.YesNo(FalloutMonoPrompt, true)
			};

			ConversionResult result = await RunWithStatusAsync(
				"Conversion ACM vers audio...",
				() => converter.ConvertAcmToAudioAsync(inputPath, outputPath, format, options)).ConfigureAwait(false);
			ConsoleReport.WriteResult(result);
		}

		private async Task ToAcmAsync()
		{
			AnsiConsole.Write(new Panel(new Markup(
				"[cyan]Les ACM Fallout sont generes en mono par defaut pour eviter une lecture acceleree en jeu.[/]"))
				.Header("Audio vers ACM")
				.BorderColor(Color.Cyan1));

			String inputPath = Prompts.ExistingPath("Fichier audio source");
			String outputPath = Prompts.OutputPath("Fichier ACM cible", Path.ChangeExtension(inputPath, ".acm"));
			AcmEncodeOptions options = new()
			{
				SampleRate = Prompts.Integer("Frequence cible", AcmEncodeOptions.DefaultSampleRate, AcmEncodeOptions.MinSampleRate, AcmEncodeOptions.MaxSampleRate),
				ForceMono = true,
				Overwrite = Prompts.YesNo("Ecraser le fichier cible si besoin ?", true)
			};

			ConversionResult result = await RunWithStatusAsync(
				"Conversion audio vers ACM...",
				() => converter.ConvertAudioToAcmAsync(inputPath, outputPath, options)).ConfigureAwait(false);
			ConsoleReport.WriteResult(result);
		}

		private static AudioFormat PromptOutputFormat(String outputPath)
		{
			AudioFormat automatic = AudioFormats.FromPath(outputPath) is { } inferred && inferred != AudioFormat.Acm
				? inferred
				: AudioFormat.Wav;

			AudioFormat choice = AnsiConsole.Prompt(
				new SelectionPrompt<AudioFormat>()
					.Title("Format de sortie")
					.UseConverter(format => format == AudioFormat.Auto ? $"Automatique ({automatic})" : format.ToString())
					.AddChoices([AudioFormat.Auto, .. AudioFormats.Exportable]));

			return choice == AudioFormat.Auto ? automatic : choice;
		}

		private static Task<ConversionResult> RunWithStatusAsync(String label, Func<Task<ConversionResult>> action)
		{
			return AnsiConsole.Status()
				.Spinner(Spinner.Known.Dots)
				.SpinnerStyle(Style.Parse("cyan"))
				.StartAsync(label, _ => action());
		}

		private static String Describe(MenuAction action)
		{
			return action switch
			{
				MenuAction.Inspect => "Inspecter un ACM",
				MenuAction.ToAudio => "Convertir ACM vers audio",
				MenuAction.ToAcm => "Convertir audio vers ACM",
				MenuAction.Quit => "Quitter",
				_ => action.ToString()
			};
		}
	}
}