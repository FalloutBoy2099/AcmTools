namespace AcmTools.Cli.Localization.Languages
{
	using System;
	using System.Collections.Generic;

	internal static class PolishTexts
	{
		public static IReadOnlyDictionary<String, String> Texts { get; } = new Dictionary<String, String>(StringComparer.Ordinal)
		{
			["MainMenuTitle"] = "Co chcesz zrobić?",
			["InspectAction"] = "Sprawdź ACM",
			["ToAudioAction"] = "Konwertuj ACM na audio",
			["ToAcmAction"] = "Konwertuj audio na ACM",
			["LanguageAction"] = "Zmień język",
			["QuitAction"] = "Zakończ",
			["LanguageTitle"] = "Język",
			["SourceAcm"] = "Źródłowy plik ACM",
			["TargetAudio"] = "Docelowy plik audio",
			["SourceAudio"] = "Źródłowy plik audio",
			["TargetAcm"] = "Docelowy plik ACM",
			["FormatTitle"] = "Format wyjściowy",
			["AutoFormat"] = "Automatycznie ({0})",
			["FileNotFound"] = "Nie znaleziono pliku.",
			["RequiredPath"] = "Ścieżka jest wymagana.",
			["Yes"] = "Tak",
			["No"] = "Nie"
		};
	}
}