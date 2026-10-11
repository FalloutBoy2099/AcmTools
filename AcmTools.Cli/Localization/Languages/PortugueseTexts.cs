namespace AcmTools.Cli.Localization.Languages
{
	using System;
	using System.Collections.Generic;

	internal static class PortugueseTexts
	{
		public static IReadOnlyDictionary<String, String> Texts { get; } = new Dictionary<String, String>(StringComparer.Ordinal)
		{
			["MainMenuTitle"] = "O que deseja fazer?",
			["InspectAction"] = "Inspecionar um ACM",
			["ToAudioAction"] = "Converter ACM para áudio",
			["ToAcmAction"] = "Converter áudio para ACM",
			["LanguageAction"] = "Alterar idioma",
			["QuitAction"] = "Sair",
			["LanguageTitle"] = "Idioma",
			["SourceAcm"] = "Arquivo ACM de origem",
			["TargetAudio"] = "Arquivo de áudio de destino",
			["SourceAudio"] = "Arquivo de áudio de origem",
			["TargetAcm"] = "Arquivo ACM de destino",
			["FormatTitle"] = "Formato de saída",
			["AutoFormat"] = "Automático ({0})",
			["FileNotFound"] = "Arquivo não encontrado.",
			["RequiredPath"] = "Caminho obrigatório.",
			["Yes"] = "Sim",
			["No"] = "Não"
		};
	}
}