namespace AcmTools.Cli.Localization
{
	using System;
	using System.Text.RegularExpressions;

	internal static partial class AppText
	{
		// The conversion steps are produced in English by the library; they are translated here by pattern.
		private static readonly (Regex Pattern, String Replacement)[] FrenchSteps =
		[
			(Step(@"Decoded ACM to (\d+) Hz, (\d+) channel\(s\), 16-bit PCM\."), "ACM décodé en PCM 16 bits : $1 Hz, $2 canal/canaux."),
			(Step(@"Wrote WAV directly from the integrated PCM writer\."), "Écriture WAV directe avec l'écrivain PCM intégré."),
			(Step(@"Encoded (\w+) with system FFmpeg\."), "Encodage $1 avec le FFmpeg du système."),
			(Step(@"Read WAV directly with the integrated PCM reader\."), "Lecture WAV directe avec le lecteur PCM intégré."),
			(Step(@"Decoded (\w+) to PCM with system FFmpeg\."), "Décodage $1 en PCM avec le FFmpeg du système."),
			(Step(@"Downmixed source audio to mono for Fallout ACM playback speed\."), "Audio source mixé en mono pour conserver la vitesse de lecture des ACM Fallout."),
			(Step(@"Resampled WAV source to (\d+) Hz with system FFmpeg\."), "Source WAV rééchantillonnée à $1 Hz avec le FFmpeg du système."),
			(Step(@"Wrote mono ACM at (\d+) Hz with the integrated encoder\."), "ACM mono écrit à $1 Hz avec l'encodeur intégré.")
		];

		public static String LocalizeStep(String step)
		{
			if (Current != CliLanguage.French)
			{
				return step;
			}

			foreach ((Regex pattern, String replacement) in FrenchSteps)
			{
				if (pattern.IsMatch(step))
				{
					return pattern.Replace(step, replacement);
				}
			}

			return step;
		}

		private static Regex Step(String pattern)
		{
			return new Regex($"^{pattern}$", RegexOptions.CultureInvariant);
		}
	}
}
