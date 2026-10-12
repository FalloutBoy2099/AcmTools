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

		/// <summary>File extensions (lower case, with the dot) of the audio files that can be converted to ACM.</summary>
		public static IReadOnlyList<String> SourceExtensions { get; } = [".wav", ".mp3", ".aac", ".m4a", ".flac", ".ape", ".ogg", ".opus", ".wma", ".mpc"];

		/// <summary>Extension (lower case, with the dot) used when writing a file of the given format.</summary>
		public static String DefaultExtension(AudioFormat format)
		{
			return format switch
			{
				AudioFormat.Acm => ".acm",
				AudioFormat.Auto => throw new ArgumentOutOfRangeException(nameof(format), "A concrete format is required."),
				_ => "." + format.ToString().ToLowerInvariant()
			};
		}

		/// <summary>Parses a format name or extension such as "ogg", ".mp3" or "m4a".</summary>
		public static Boolean TryParse(String? name, out AudioFormat format)
		{
			format = AudioFormat.Auto;
			String? trimmed = name?.Trim().TrimStart('.');
			if (String.IsNullOrEmpty(trimmed))
			{
				return false;
			}

			AudioFormat? parsed = FromPath("x." + trimmed.ToLowerInvariant());
			if (parsed is null && String.Equals(trimmed, "vorbis", StringComparison.OrdinalIgnoreCase))
			{
				parsed = AudioFormat.Ogg;
			}

			if (parsed is null || parsed == AudioFormat.Acm)
			{
				return false;
			}

			format = parsed.Value;
			return true;
		}

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