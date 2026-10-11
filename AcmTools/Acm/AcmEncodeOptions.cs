namespace AcmTools.Acm
{
	using System;

	public sealed record AcmEncodeOptions
	{
		public const Int32 DefaultSampleRate = 22050;

		public const Int32 MinSampleRate = 4096;

		public const Int32 MaxSampleRate = UInt16.MaxValue;

		public Int32 SampleRate { get; init; } = DefaultSampleRate;

		public Boolean ForceMono { get; init; } = true;

		public Boolean Overwrite { get; init; } = true;
	}
}
