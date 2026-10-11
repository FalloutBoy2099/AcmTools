namespace AcmTools.Conversion
{
	using System;
	using System.Collections.Generic;
	using System.IO;

	public static class AudioFormats
	{
		/// <summary>Formats that can be exported from a decoded ACM (everything except <see cref="AudioFormat.Auto"/> and <see cref="AudioFormat.Acm"/>).</summary>
		public static IReadOnlyList<AudioFormat> Exportable { get; } =
		[
			AudioFormat.Wav,
			AudioFormat.Mp3,
			AudioFormat.Aac,
			AudioFormat.Flac,
			AudioFormat.Ape,
			AudioFormat.Ogg,
			AudioFormat.Opus,
			AudioFormat.Wma,
			AudioFormat.Mpc
		];

		/// <summary>Returns the format matching the file extension of <paramref name="path"/>, or <c>null</c> when it is unknown.</summary>
		public static AudioFormat? FromPath(String path)
		{
			return Path.GetExtension(path).ToLowerInvariant() switch
			{
				".acm" => AudioFormat.Acm,
				".wav" => AudioFormat.Wav,
				".mp3" => AudioFormat.Mp3,
				".aac" or ".m4a" => AudioFormat.Aac,
				".flac" => AudioFormat.Flac,
				".ape" => AudioFormat.Ape,
				".ogg" => AudioFormat.Ogg,
				".opus" => AudioFormat.Opus,
				".wma" => AudioFormat.Wma,
				".mpc" => AudioFormat.Mpc,
				_ => null
			};
		}
	}
}