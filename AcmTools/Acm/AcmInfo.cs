namespace AcmTools.Acm
{
	using System;

	public sealed record AcmInfo(
		Int32 SampleCount,
		Int16 HeaderChannels,
		Int16 Channels,
		Int32 SampleRate,
		Int32 Level,
		Int32 Rows);
}