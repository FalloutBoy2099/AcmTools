namespace AcmTools.Cli.Localization.Languages
{
	using System;
	using System.Collections.Generic;

	internal static class SwedishTexts
	{
		public static IReadOnlyDictionary<String, String> Texts { get; } = new Dictionary<String, String>(StringComparer.Ordinal)
		{
			["MainMenuTitle"] = "Vad vill du göra?",
			["InspectAction"] = "Inspektera en ACM",
			["ToAudioAction"] = "Konvertera ACM till ljud",
			["ToAcmAction"] = "Konvertera ljud till ACM",
			["LanguageAction"] = "Ändra språk",
			["QuitAction"] = "Avsluta",
			["LanguageTitle"] = "Språk",
			["SourceAcm"] = "ACM-fil (källa)",
			["TargetAudio"] = "Ljudfil (mål)",
			["SourceAudio"] = "Ljudfil (källa)",
			["TargetAcm"] = "ACM-fil (mål)",
			["FormatTitle"] = "Utdataformat",
			["AutoFormat"] = "Automatiskt ({0})",
			["FileNotFound"] = "Filen hittades inte.",
			["RequiredPath"] = "Sökväg krävs.",
			["Yes"] = "Ja",
			["No"] = "Nej"
		};
	}
}