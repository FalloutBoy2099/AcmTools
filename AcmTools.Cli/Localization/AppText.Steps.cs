namespace AcmTools.Cli.Localization
{
	using System;
	using System.Linq;
	using System.Text.RegularExpressions;

	internal static partial class AppText
	{
		// The conversion steps are produced in English by the library. Each pattern maps one of them to a text key;
		// the captured groups become {0}, {1}... of that text in the current language.
		private static readonly (Regex Pattern, String Key)[] StepPatterns =
		[
			(Step(@"Decoded ACM to (\d+) Hz, (\d+) channel\(s\), 16-bit PCM\."), "StepDecodedAcm"),
			(Step(@"Wrote WAV directly from the integrated PCM writer\."), "StepWroteWav"),
			(Step(@"Encoded (\w+) with system FFmpeg\."), "StepEncodedFfmpeg"),
			(Step(@"Read WAV directly with the integrated PCM reader\."), "StepReadWav"),
			(Step(@"Decoded (\w+) to PCM with system FFmpeg\."), "StepDecodedFfmpeg"),
			(Step(@"Downmixed source audio to mono for Fallout ACM playback speed\."), "StepDownmixed"),
			(Step(@"Resampled WAV source to (\d+) Hz with system FFmpeg\."), "StepResampled"),
			(Step(@"Wrote mono ACM at (\d+) Hz with the integrated encoder\."), "StepWroteAcm")
		];

		public static String LocalizeStep(String step)
		{
			foreach ((Regex pattern, String key) in StepPatterns)
			{
				Match match = pattern.Match(step);
				if (match.Success)
				{
					return Format(key, match.Groups.Cast<Group>().Skip(1).Select(group => (Object)group.Value).ToArray());
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