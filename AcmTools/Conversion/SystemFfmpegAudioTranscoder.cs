namespace AcmTools.Conversion
{
	using System;
	using System.Collections.Generic;
	using System.ComponentModel;
	using System.Diagnostics;
	using System.Globalization;
	using System.IO;
	using System.Threading;

	internal sealed class SystemFfmpegAudioTranscoder
	{
		private const String MissingFfmpegMessage =
			"Audio conversion for compressed formats requires FFmpeg installed and available in PATH. " +
			"Windows: winget install Gyan.FFmpeg; Debian/Ubuntu: sudo apt install ffmpeg; macOS: brew install ffmpeg.";

		private static readonly IReadOnlyDictionary<AudioFormat, String[]> EncoderArguments = new Dictionary<AudioFormat, String[]>
		{
			[AudioFormat.Mp3] = ["-c:a", "libmp3lame", "-q:a", "2"],
			[AudioFormat.Aac] = ["-c:a", "aac", "-b:a", "160k"],
			[AudioFormat.Flac] = ["-c:a", "flac"],
			[AudioFormat.Ape] = ["-c:a", "ape"],
			[AudioFormat.Ogg] = ["-c:a", "libvorbis", "-q:a", "5"],
			[AudioFormat.Opus] = ["-c:a", "libopus", "-b:a", "96k"],
			[AudioFormat.Wma] = ["-c:a", "wmav2", "-b:a", "160k"],
			[AudioFormat.Mpc] = ["-c:a", "mpc7"]
		};

		public Task DecodeToWavAsync(String inputPath, String outputWavPath, Int32? sampleRate, Boolean mono, CancellationToken cancellationToken)
		{
			List<String> args = ["-vn"];
			if (mono)
			{
				args.AddRange(["-ac", "1"]);
			}

			if (sampleRate.HasValue)
			{
				args.AddRange(["-ar", sampleRate.Value.ToString(CultureInfo.InvariantCulture)]);
			}

			args.AddRange(["-c:a", "pcm_s16le"]);
			return RunAsync(inputPath, outputWavPath, args, cancellationToken);
		}

		public Task EncodeFromWavAsync(String inputWavPath, String outputPath, AudioFormat format, CancellationToken cancellationToken)
		{
			if (!EncoderArguments.TryGetValue(format, out String[]? args))
			{
				throw new NotSupportedException($"FFmpeg export is not used for {format}.");
			}

			return RunAsync(inputWavPath, outputPath, args, cancellationToken);
		}

		private static async Task RunAsync(String inputPath, String outputPath, IEnumerable<String> codecArguments, CancellationToken cancellationToken)
		{
			String? directory = Path.GetDirectoryName(Path.GetFullPath(outputPath));
			if (!String.IsNullOrWhiteSpace(directory))
			{
				Directory.CreateDirectory(directory);
			}

			using Process process = new();
			process.StartInfo = new ProcessStartInfo
			{
				FileName = "ffmpeg",
				UseShellExecute = false,
				RedirectStandardError = true,
				RedirectStandardOutput = true,
				CreateNoWindow = true
			};

			process.StartInfo.ArgumentList.Add("-nostdin");
			process.StartInfo.ArgumentList.Add("-y");
			process.StartInfo.ArgumentList.Add("-hide_banner");
			process.StartInfo.ArgumentList.Add("-loglevel");
			process.StartInfo.ArgumentList.Add("error");
			process.StartInfo.ArgumentList.Add("-i");
			process.StartInfo.ArgumentList.Add(inputPath);

			foreach (String arg in codecArguments)
			{
				process.StartInfo.ArgumentList.Add(arg);
			}

			process.StartInfo.ArgumentList.Add(outputPath);

			try
			{
				process.Start();
			}
			catch (Exception ex) when (ex is FileNotFoundException or Win32Exception)
			{
				throw new NotSupportedException(MissingFfmpegMessage, ex);
			}

			// Both pipes are drained concurrently so that a chatty FFmpeg can never block on a full pipe.
			Task<String> standardError = process.StandardError.ReadToEndAsync(CancellationToken.None);
			Task<String> standardOutput = process.StandardOutput.ReadToEndAsync(CancellationToken.None);

			try
			{
				await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
			}
			catch (OperationCanceledException)
			{
				KillQuietly(process);
				DeleteQuietly(outputPath);
				throw;
			}

			String errorText = await standardError.ConfigureAwait(false);
			String outputText = await standardOutput.ConfigureAwait(false);
			if (process.ExitCode != 0)
			{
				String details = String.IsNullOrWhiteSpace(errorText) ? outputText : errorText;
				throw new InvalidOperationException($"FFmpeg audio conversion failed with exit code {process.ExitCode}. {details.Trim()}");
			}
		}

		private static void KillQuietly(Process process)
		{
			try
			{
				if (!process.HasExited)
				{
					process.Kill(true);
				}
			}
			catch (InvalidOperationException)
			{
			}
		}

		private static void DeleteQuietly(String path)
		{
			try
			{
				File.Delete(path);
			}
			catch (IOException)
			{
			}
			catch (UnauthorizedAccessException)
			{
			}
		}
	}
}