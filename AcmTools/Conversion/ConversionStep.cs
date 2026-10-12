namespace AcmTools.Conversion
{
	using System;
	using System.Collections.Generic;
	using System.Globalization;
	using System.Linq;

	public enum ConversionStepKind
	{
		DecodedAcm,
		WroteWav,
		EncodedFfmpeg,
		ReadWav,
		DecodedFfmpeg,
		Downmixed,
		Resampled,
		KeptStereo,
		WroteAcm
	}

	/// <summary>
	/// One thing the converter did, as a code plus its parameters so that front ends can translate it.
	/// <see cref="Message"/> and <see cref="ToString"/> give the English text.
	/// </summary>
	public sealed record ConversionStep(ConversionStepKind Kind, IReadOnlyList<String> Arguments)
	{
		public String Message => String.Format(CultureInfo.InvariantCulture, Template(Kind), [.. Arguments]);

		public static ConversionStep Create(ConversionStepKind kind, params Object[] arguments)
		{
			return new ConversionStep(kind, [.. arguments.Select(argument => Convert.ToString(argument, CultureInfo.InvariantCulture) ?? String.Empty)]);
		}

		public override String ToString()
		{
			return Message;
		}

		private static String Template(ConversionStepKind kind)
		{
			return kind switch
			{
				ConversionStepKind.DecodedAcm => "Decoded ACM to {0} Hz, {1} channel(s), 16-bit PCM.",
				ConversionStepKind.WroteWav => "Wrote WAV directly from the integrated PCM writer.",
				ConversionStepKind.EncodedFfmpeg => "Encoded {0} with system FFmpeg.",
				ConversionStepKind.ReadWav => "Read WAV directly with the integrated PCM reader.",
				ConversionStepKind.DecodedFfmpeg => "Decoded {0} to PCM with system FFmpeg.",
				ConversionStepKind.Downmixed => "Downmixed source audio to mono for Fallout ACM playback speed.",
				ConversionStepKind.Resampled => "Resampled WAV source to {0} Hz with system FFmpeg.",
				ConversionStepKind.KeptStereo => "Kept both channels: {0} s of audio is treated as music.",
				ConversionStepKind.WroteAcm => "Wrote {1}-channel ACM at {0} Hz with the integrated encoder.",
				_ => kind.ToString()
			};
		}
	}
}