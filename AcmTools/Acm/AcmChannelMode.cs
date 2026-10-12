namespace AcmTools.Acm
{
	/// <summary>How many channels an ACM is read with, or written with.</summary>
	public enum AcmChannelMode
	{
		/// <summary>Audio longer than the stereo threshold (60 s by default) is music and keeps its two channels, everything else is mono.</summary>
		Auto,

		/// <summary>Always mono: what the game does for speech and sound effects, whatever their header says.</summary>
		Mono,

		/// <summary>Always the channels of the file (stereo for a stereo source).</summary>
		Stereo
	}
}