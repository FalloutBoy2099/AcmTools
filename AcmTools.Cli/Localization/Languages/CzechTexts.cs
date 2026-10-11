namespace AcmTools.Cli.Localization.Languages
{
	using System;
	using System.Collections.Generic;

	internal static class CzechTexts
	{
		public static IReadOnlyDictionary<String, String> Texts { get; } = new Dictionary<String, String>(StringComparer.Ordinal)
		{
			["MainMenuTitle"] = "Co chceš udělat?",
			["InspectAction"] = "Zkontrolovat ACM",
			["ToAudioAction"] = "Převést ACM na audio",
			["ToAcmAction"] = "Převést audio na ACM",
			["LanguageAction"] = "Změnit jazyk",
			["QuitAction"] = "Konec",
			["LanguageTitle"] = "Jazyk",
			["SourceAcm"] = "Zdrojový soubor ACM",
			["TargetAudio"] = "Cílový zvukový soubor",
			["SourceAudio"] = "Zdrojový zvukový soubor",
			["TargetAcm"] = "Cílový soubor ACM",
			["FormatTitle"] = "Výstupní formát",
			["AutoFormat"] = "Automaticky ({0})",
			["FileNotFound"] = "Soubor nenalezen.",
			["RequiredPath"] = "Cesta je povinná.",
			["Yes"] = "Ano",
			["No"] = "Ne"
		};
	}
}