namespace AcmTools.Acm
{
	using System;

	public sealed record AcmDecodeOptions
	{
		public const Double DefaultStereoThresholdSeconds = 60;

		/// <summary>
		/// Many files of the game are tagged stereo although they are mono, so the header cannot be trusted blindly.
		/// In <see cref="AcmChannelMode.Auto"/> only files longer than <see cref="StereoThresholdSeconds"/> (counted with the
		/// channels of the header), which are the music tracks, are read as stereo.
		/// </summary>
		public AcmChannelMode Channels { get; init; } = AcmChannelMode.Auto;

		/// <summary>Duration above which <see cref="AcmChannelMode.Auto"/> considers a stereo-tagged file to be music.</summary>
		public Double StereoThresholdSeconds { get; init; } = DefaultStereoThresholdSeconds;

		/// <summary>Whether a conversion may replace an existing output file.</summary>
		public Boolean Overwrite { get; init; } = true;
	}
}