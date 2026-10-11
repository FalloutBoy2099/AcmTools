namespace AcmTools.Cli.Localization.Languages
{
	using System;
	using System.Collections.Generic;

	internal static class SpanishTexts
	{
		public static IReadOnlyDictionary<String, String> Texts { get; } = new Dictionary<String, String>(StringComparer.Ordinal)
		{
			["MainMenuTitle"] = "¿Qué quieres hacer?",
			["InspectAction"] = "Inspeccionar un ACM",
			["ToAudioAction"] = "Convertir ACM a audio",
			["ToAcmAction"] = "Convertir audio a ACM",
			["LanguageAction"] = "Cambiar idioma",
			["QuitAction"] = "Salir",
			["LanguageTitle"] = "Idioma",
			["SourceAcm"] = "Archivo ACM de origen",
			["TargetAudio"] = "Archivo de audio de destino",
			["SourceAudio"] = "Archivo de audio de origen",
			["TargetAcm"] = "Archivo ACM de destino",
			["FormatTitle"] = "Formato de salida",
			["AutoFormat"] = "Automático ({0})",
			["FileNotFound"] = "Archivo no encontrado.",
			["RequiredPath"] = "Ruta obligatoria.",
			["Yes"] = "Sí",
			["No"] = "No"
		};
	}
}