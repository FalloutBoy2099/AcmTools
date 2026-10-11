namespace AcmTools.Cli.Localization.Languages
{
	using System;
	using System.Collections.Generic;

	internal static class GermanTexts
	{
		public static IReadOnlyDictionary<String, String> Texts { get; } = new Dictionary<String, String>(StringComparer.Ordinal)
		{
			["MainMenuTitle"] = "Was möchtest du tun?",
			["InspectAction"] = "ACM prüfen",
			["ToAudioAction"] = "ACM in Audio konvertieren",
			["ToAcmAction"] = "Audio in ACM konvertieren",
			["LanguageAction"] = "Sprache ändern",
			["QuitAction"] = "Beenden",
			["LanguageTitle"] = "Sprache",
			["SourceAcm"] = "Quell-ACM-Datei",
			["TargetAudio"] = "Ziel-Audiodatei",
			["SourceAudio"] = "Quell-Audiodatei",
			["TargetAcm"] = "Ziel-ACM-Datei",
			["FormatTitle"] = "Ausgabeformat",
			["AutoFormat"] = "Automatisch ({0})",
			["FileNotFound"] = "Datei nicht gefunden.",
			["RequiredPath"] = "Pfad ist erforderlich.",
			["Yes"] = "Ja",
			["No"] = "Nein"
		};
	}
}