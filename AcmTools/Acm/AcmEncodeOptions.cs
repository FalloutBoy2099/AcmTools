namespace AcmTools.Acm
{
	using System;

	public sealed record AcmEncodeOptions
	{
		public const Int32 DefaultSampleRate = 22050;

		public const Int32 MinSampleRate = 4096;

		public const Int32 MaxSampleRate = UInt16.MaxValue;

		/// <summary>Level of the original Fallout files: 128 columns of 16 rows, i.e. blocks of 2048 samples.</summary>
		public const Int32 DefaultTransformLevel = 7;

		public const Int32 MaxTransformLevel = 10;

		public const Double DefaultTargetSnrDb = 30;

		public const Double DefaultStereoThresholdSeconds = AcmDecodeOptions.DefaultStereoThresholdSeconds;

		/// <summary>Smallest sample width the encoder can use (strongest quantization).</summary>
		public const Int32 MinBitsPerSample = 3;

		/// <summary>Sample width that stores 16-bit audio without any loss.</summary>
		public const Int32 LosslessBitsPerSample = 16;

		public Int32 SampleRate { get; init; } = DefaultSampleRate;

		/// <summary>
		/// Channels written for a stereo source. <see cref="AcmChannelMode.Auto"/> keeps the two channels of music (audio
		/// longer than <see cref="StereoThresholdSeconds"/>) and mixes everything shorter down to mono, like the speech and
		/// sound effects of the game.
		/// </summary>
		public AcmChannelMode Channels { get; init; } = AcmChannelMode.Auto;

		/// <summary>Duration above which <see cref="AcmChannelMode.Auto"/> considers a stereo source to be music.</summary>
		public Double StereoThresholdSeconds { get; init; } = DefaultStereoThresholdSeconds;

		public Boolean Overwrite { get; init; } = true;

		/// <summary>
		/// Number of sub-band stages of the codec (0 to <see cref="MaxTransformLevel"/>). With the default, audio goes
		/// through the same transform as the original Interplay encoder and is stored as quantized coefficients (lossy,
		/// several times smaller). 0 stores the samples as they are in linear blocks (see <see cref="MaxBitsPerSample"/>).
		/// </summary>
		public Int32 TransformLevel { get; init; } = DefaultTransformLevel;

		/// <summary>
		/// Signal-to-noise ratio, in dB, the quantizer aims for in every block when <see cref="TransformLevel"/> is not 0.
		/// Higher values give a better sound and a bigger file.
		/// </summary>
		public Double TargetSnrDb { get; init; } = DefaultTargetSnrDb;

		/// <summary>
		/// Upper bound of the bits stored for the largest value of a block. Each block uses as few bits as it needs,
		/// so the default (<see cref="LosslessBitsPerSample"/>) never limits the quality; lower values quantize loud
		/// blocks more to shrink the file (with <see cref="TransformLevel"/> 0 this is the only quality setting).
		/// </summary>
		public Int32 MaxBitsPerSample { get; init; } = LosslessBitsPerSample;

		/// <summary>Number of channels (1 or 2) that will be written for a source with the given channels and duration.</summary>
		public Int32 ResolveChannels(Int32 sourceChannels, Double durationSeconds)
		{
			if (sourceChannels != 2)
			{
				return 1;
			}

			return Channels switch
			{
				AcmChannelMode.Mono => 1,
				AcmChannelMode.Stereo => 2,
				_ => durationSeconds > StereoThresholdSeconds ? 2 : 1
			};
		}
	}
}