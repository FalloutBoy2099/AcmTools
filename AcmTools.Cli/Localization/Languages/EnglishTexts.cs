namespace AcmTools.Cli.Localization.Languages
{
	using System;
	using System.Collections.Generic;

	internal static class EnglishTexts
	{
		public static IReadOnlyDictionary<String, String> Texts { get; } = new Dictionary<String, String>(StringComparer.Ordinal)
		{
			["ToolkitTagline"] = "Fallout/Interplay ACM audio converter",
			["MainMenuTitle"] = "What do you want to do?",
			["InspectAction"] = "Inspect an ACM",
			["ToAudioAction"] = "Convert ACM to audio",
			["ToAcmAction"] = "Convert audio to ACM",
			["LanguageAction"] = "Change language",
			["QuitAction"] = "Quit",
			["LanguageTitle"] = "Language",
			["LanguageChanged"] = "Language changed to {0}.",
			["SourceAcm"] = "Source ACM file",
			["TargetAudio"] = "Target audio file",
			["SourceAudio"] = "Source audio file",
			["TargetAcm"] = "Target ACM file",
			["FormatTitle"] = "Output format",
			["AutoFormat"] = "Automatic ({0})",
			["FalloutMonoConfirm"] = "Fallout mono mode (recommended for Fallout ACM files)?",
			["SampleRatePrompt"] = "Target sample rate",
			["OverwriteConfirm"] = "Overwrite the target file if needed?",
			["ToAudioStatus"] = "Converting ACM to audio...",
			["ToAcmStatus"] = "Converting audio to ACM...",
			["ToAcmHeader"] = "Audio to ACM",
			["ToAcmIntro"] = "Fallout ACM files are generated in mono by default to avoid sped-up playback in game.",
			["Yes"] = "Yes",
			["No"] = "No",
			["FileNotFound"] = "File not found.",
			["RequiredPath"] = "The path is required.",
			["RangeError"] = "Expected a value between {0} and {1}.",
			["FieldColumn"] = "Field",
			["ValueColumn"] = "Value",
			["Samples"] = "Samples",
			["HeaderChannels"] = "Header channels",
			["EffectiveChannels"] = "Effective channels",
			["SampleRate"] = "Sample rate",
			["AcmLevel"] = "ACM level",
			["Rows"] = "Rows",
			["HelpFalloutNote"] = "Fallout mode is mono by default. Use --trust-header-channels only for non-Fallout ACM files.",
			["UnknownCommand"] = "Unknown command: {0}",
			["UnknownOption"] = "Unknown option: {0}",
			["MissingOptionValue"] = "Missing value for option {0}.",
			["ExpectedArguments"] = "Expected {0} argument(s), got {1}.",
			["InvalidIntegerOption"] = "Option {0} expects an integer between {1} and {2}, got '{3}'.",
			["Done"] = "Done",
			["StepDecodedAcm"] = "Decoded ACM to {0} Hz, {1} channel(s), 16-bit PCM.",
			["StepWroteWav"] = "Wrote WAV directly from the integrated PCM writer.",
			["StepEncodedFfmpeg"] = "Encoded {0} with system FFmpeg.",
			["StepReadWav"] = "Read WAV directly with the integrated PCM reader.",
			["StepDecodedFfmpeg"] = "Decoded {0} to PCM with system FFmpeg.",
			["StepDownmixed"] = "Downmixed source audio to mono for Fallout ACM playback speed.",
			["StepResampled"] = "Resampled WAV source to {0} Hz with system FFmpeg.",
			["StepWroteAcm"] = "Wrote mono ACM at {0} Hz with the integrated encoder."
		};
	}
}