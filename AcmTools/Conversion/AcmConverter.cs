namespace AcmTools.Conversion
{
	using System;
	using System.Collections.Generic;
	using System.IO;
	using Acm;
	using Audio;

	public sealed class AcmConverter
	{
		private readonly SystemFfmpegAudioTranscoder ffmpeg = new();

		public Task<ConversionResult> ConvertAcmToAudioAsync(String inputAcmPath, String outputAudioPath, AudioFormat outputFormat = AudioFormat.Auto, AcmDecodeOptions? options = null)
		{
			EnsureInputFile(inputAcmPath);
			EnsureOutputCanBeWritten(outputAudioPath, true);
			outputFormat = ResolveFormat(outputAudioPath, outputFormat);
			if (outputFormat == AudioFormat.Acm)
			{
				throw new NotSupportedException("Use a non-ACM output format for ACM decoding.");
			}

			List<String> steps = [];
			PcmAudioTrack pcm = new AcmDecoder().Decode(inputAcmPath, options);
			steps.Add($"Decoded ACM to {pcm.SampleRate} Hz, {pcm.Channels} channel(s), 16-bit PCM.");

			if (outputFormat == AudioFormat.Wav)
			{
				new WavPcmWriter().Write(outputAudioPath, pcm);
				steps.Add("Wrote WAV directly from the integrated PCM writer.");
			}
			else
			{
				using TemporaryFile tempWav = new();
				new WavPcmWriter().Write(tempWav.Path, pcm);
				ffmpeg.EncodeFromWav(tempWav.Path, outputAudioPath, outputFormat);
				steps.Add($"Encoded {outputFormat} with system FFmpeg.");
			}

			return Task.FromResult(new ConversionResult(inputAcmPath, outputAudioPath, steps));
		}

		public Task<ConversionResult> ConvertAudioToAcmAsync(String inputAudioPath, String outputAcmPath, AcmEncodeOptions? options = null)
		{
			options ??= new AcmEncodeOptions();
			EnsureInputFile(inputAudioPath);
			EnsureOutputCanBeWritten(outputAcmPath, options.Overwrite);
			AudioFormat inputFormat = ResolveFormat(inputAudioPath, AudioFormat.Auto);
			List<String> steps = [];

			PcmAudioTrack pcm = ReadSourcePcm(inputAudioPath, inputFormat, options, steps);

			if (options.ForceMono && pcm.Channels != 1)
			{
				pcm = pcm.ToMono16();
				steps.Add("Downmixed source audio to mono for Fallout ACM playback speed.");
			}

			if (pcm.SampleRate != options.SampleRate && inputFormat == AudioFormat.Wav)
			{
				pcm = ResampleWithFfmpeg(pcm, options);
				steps.Add($"Resampled WAV source to {options.SampleRate} Hz with system FFmpeg.");
			}

			new AcmEncoder().Encode(outputAcmPath, pcm, options);
			steps.Add($"Wrote mono ACM at {pcm.SampleRate} Hz with the integrated encoder.");
			return Task.FromResult(new ConversionResult(inputAudioPath, outputAcmPath, steps));
		}

		public AcmInfo Inspect(String path, AcmDecodeOptions? options = null)
		{
			EnsureInputFile(path);
			return new AcmDecoder().Inspect(path, options);
		}

		private PcmAudioTrack ReadSourcePcm(String inputAudioPath, AudioFormat inputFormat, AcmEncodeOptions options, List<String> steps)
		{
			if (inputFormat == AudioFormat.Wav)
			{
				steps.Add("Read WAV directly with the integrated PCM reader.");
				return new WavPcmReader().Read(inputAudioPath);
			}

			using TemporaryFile tempWav = new();
			ffmpeg.DecodeToWav(inputAudioPath, tempWav.Path, options.SampleRate, options.ForceMono);
			steps.Add($"Decoded {inputFormat} to PCM with system FFmpeg.");
			return new WavPcmReader().Read(tempWav.Path);
		}

		private PcmAudioTrack ResampleWithFfmpeg(PcmAudioTrack pcm, AcmEncodeOptions options)
		{
			using TemporaryFile tempIn = new("acmtools-in");
			using TemporaryFile tempOut = new("acmtools-out");
			new WavPcmWriter().Write(tempIn.Path, pcm);
			ffmpeg.DecodeToWav(tempIn.Path, tempOut.Path, options.SampleRate, options.ForceMono);
			return new WavPcmReader().Read(tempOut.Path);
		}

		private static AudioFormat ResolveFormat(String path, AudioFormat format)
		{
			if (format != AudioFormat.Auto)
			{
				return format;
			}

			return AudioFormats.FromPath(path)
				?? throw new NotSupportedException($"Cannot infer audio format from extension: {path}");
		}

		private static void EnsureInputFile(String path)
		{
			if (!File.Exists(path))
			{
				throw new FileNotFoundException("Input file was not found.", path);
			}
		}

		private static void EnsureOutputCanBeWritten(String path, Boolean overwrite)
		{
			if (File.Exists(path) && !overwrite)
			{
				throw new IOException($"Output file already exists: {path}");
			}
		}
	}
}