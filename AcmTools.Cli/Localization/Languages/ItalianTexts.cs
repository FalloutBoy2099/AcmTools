namespace AcmTools.Cli.Localization.Languages
{
	using System;
	using System.Collections.Generic;

	internal static class ItalianTexts
	{
		public static IReadOnlyDictionary<String, String> Texts { get; } = new Dictionary<String, String>(StringComparer.Ordinal)
		{
			["MainMenuTitle"] = "Cosa vuoi fare?",
			["InspectAction"] = "Ispeziona un ACM",
			["ToAudioAction"] = "Converti ACM in audio",
			["ToAcmAction"] = "Converti audio in ACM",
			["LanguageAction"] = "Cambia lingua",
			["QuitAction"] = "Esci",
			["LanguageTitle"] = "Lingua",
			["SourceAcm"] = "File ACM sorgente",
			["TargetAudio"] = "File audio di destinazione",
			["SourceAudio"] = "File audio sorgente",
			["TargetAcm"] = "File ACM di destinazione",
			["FormatTitle"] = "Formato di uscita",
			["AutoFormat"] = "Automatico ({0})",
			["FileNotFound"] = "File non trovato.",
			["RequiredPath"] = "Percorso obbligatorio.",
			["Yes"] = "Sì",
			["No"] = "No"
		};
	}
}