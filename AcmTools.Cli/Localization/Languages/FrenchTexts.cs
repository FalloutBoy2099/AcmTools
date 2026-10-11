namespace AcmTools.Cli.Localization.Languages
{
	using System;
	using System.Collections.Generic;

	internal static class FrenchTexts
	{
		public static IReadOnlyDictionary<String, String> Texts { get; } = new Dictionary<String, String>(StringComparer.Ordinal)
		{
			["ToolkitTagline"] = "Convertisseur audio ACM Fallout/Interplay",
			["MainMenuTitle"] = "Que veux-tu faire ?",
			["InspectAction"] = "Inspecter un ACM",
			["ToAudioAction"] = "Convertir ACM vers audio",
			["ToAcmAction"] = "Convertir audio vers ACM",
			["LanguageAction"] = "Modifier la langue",
			["QuitAction"] = "Quitter",
			["LanguageTitle"] = "Langue",
			["LanguageChanged"] = "Langue changée en {0}.",
			["SourceAcm"] = "Fichier ACM source",
			["TargetAudio"] = "Fichier audio cible",
			["SourceAudio"] = "Fichier audio source",
			["TargetAcm"] = "Fichier ACM cible",
			["FormatTitle"] = "Format de sortie",
			["AutoFormat"] = "Automatique ({0})",
			["FalloutMonoConfirm"] = "Mode Fallout mono (recommandé pour les ACM Fallout) ?",
			["SampleRatePrompt"] = "Fréquence cible",
			["OverwriteConfirm"] = "Écraser le fichier cible si besoin ?",
			["ToAudioStatus"] = "Conversion ACM vers audio...",
			["ToAcmStatus"] = "Conversion audio vers ACM...",
			["ToAcmHeader"] = "Audio vers ACM",
			["ToAcmIntro"] = "Les ACM Fallout sont générés en mono par défaut pour éviter une lecture accélérée en jeu.",
			["Yes"] = "Oui",
			["No"] = "Non",
			["FileNotFound"] = "Fichier introuvable.",
			["RequiredPath"] = "Le chemin est obligatoire.",
			["RangeError"] = "Valeur attendue entre {0} et {1}.",
			["FieldColumn"] = "Champ",
			["ValueColumn"] = "Valeur",
			["Samples"] = "Échantillons",
			["HeaderChannels"] = "Canaux (en-tête)",
			["EffectiveChannels"] = "Canaux effectifs",
			["SampleRate"] = "Fréquence d'échantillonnage",
			["AcmLevel"] = "Niveau ACM",
			["Rows"] = "Lignes",
			["HelpFalloutNote"] = "Le mode Fallout est mono par défaut. N'utilise --trust-header-channels que pour des ACM non Fallout.",
			["UnknownCommand"] = "Commande inconnue : {0}",
			["UnknownOption"] = "Option inconnue : {0}",
			["MissingOptionValue"] = "Valeur manquante pour l'option {0}.",
			["ExpectedArguments"] = "{0} argument(s) attendu(s), {1} reçu(s).",
			["InvalidIntegerOption"] = "L'option {0} attend un entier entre {1} et {2}, reçu « {3} ».",
			["Done"] = "Terminé",
			["StepDecodedAcm"] = "ACM décodé en PCM 16 bits : {0} Hz, {1} canal/canaux.",
			["StepWroteWav"] = "Écriture WAV directe avec l'écrivain PCM intégré.",
			["StepEncodedFfmpeg"] = "Encodage {0} avec le FFmpeg du système.",
			["StepReadWav"] = "Lecture WAV directe avec le lecteur PCM intégré.",
			["StepDecodedFfmpeg"] = "Décodage {0} en PCM avec le FFmpeg du système.",
			["StepDownmixed"] = "Audio source mixé en mono pour conserver la vitesse de lecture des ACM Fallout.",
			["StepResampled"] = "Source WAV rééchantillonnée à {0} Hz avec le FFmpeg du système.",
			["StepWroteAcm"] = "ACM mono écrit à {0} Hz avec l'encodeur intégré."
		};
	}
}