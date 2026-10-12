namespace AcmTools.Conversion
{
	using System;
	using System.Collections.Generic;
	using System.IO;
	using System.Threading;
	using Acm;
	using Audio;

	public sealed class AcmConverter
	{
		private readonly SystemFfmpegAudioTranscoder ffmpeg = new();

		public async Task<ConversionResult> ConvertAcmToAudioAsync(String inputAcmPath, String outputAudioPath, AudioFormat outputFormat = AudioFormat.Auto, AcmDecodeOptions? options = null, CancellationToken cancellationToken = default)
		{
			options ??= new AcmDecodeOptions();
			EnsureInputFile(inputAcmPath);
			EnsureOutputCanBeWritten(outputAudioPath, options.Overwrite);
			outputFormat = ResolveFormat(outputAudioPath, outputFormat);
			if (outputFormat == AudioFormat.Acm)
			{
				throw new NotSupportedException("Use a non-ACM output format for ACM decoding.");
			}

			List<ConversionStep> steps = [];
			PcmAudioTrack pcm = await Task.Run(() => new AcmDecoder().Decode(inputAcmPath, options, cancellationToken), cancellationToken).ConfigureAwait(false);
			steps.Add(ConversionStep.Create(ConversionStepKind.DecodedAcm, pcm.SampleRate, pcm.Channels));

			if (outputFormat == AudioFormat.Wav)
			{
				await Task.Run(() => new WavPcmWriter().Write(outputAudioPath, pcm), cancellationToken).ConfigureAwait(false);
				steps.Add(ConversionStep.Create(ConversionStepKind.WroteWav));
			}
			else
			{
				using TemporaryFile tempWav = new();
				await Task.Run(() => new WavPcmWriter().Write(tempWav.Path, pcm), cancellationToken).ConfigureAwait(false);
				await ffmpeg.EncodeFromWavAsync(tempWav.Path, outputAudioPath, outputFormat, cancellationToken).ConfigureAwait(false);
				steps.Add(ConversionStep.Create(ConversionStepKind.EncodedFfmpeg, outputFormat));
			}

			return new ConversionResult(inputAcmPath, outputAudioPath, steps);
		}

		public async Task<ConversionResult> ConvertAudioToAcmAsync(String inputAudioPath, String outputAcmPath, AcmEncodeOptions? options = null, CancellationToken cancellationToken = default)
		{
			options ??= new AcmEncodeOptions();
			EnsureInputFile(inputAudioPath);
			EnsureOutputCanBeWritten(outputAcmPath, options.Overwrite);
			AudioFormat inputFormat = ResolveFormat(inputAudioPath, AudioFormat.Auto);
			List<ConversionStep> steps = [];

			PcmAudioTrack pcm = await ReadSourcePcmAsync(inputAudioPath, inputFormat, options, steps, cancellationToken).ConfigureAwait(false);

			Double seconds = (Double)pcm.SampleCount / pcm.SampleRate;
			Int32 channels = options.ResolveChannels(pcm.Channels, seconds);
			if (channels == 1 && pcm.Channels != 1)
			{
				pcm = pcm.ToMono16();
				steps.Add(ConversionStep.Create(ConversionStepKind.Downmixed));
			}
			else if (channels == 2)
			{
				steps.Add(ConversionStep.Create(ConversionStepKind.KeptStereo, Math.Round(seconds)));
			}

			if (pcm.SampleRate != options.SampleRate && inputFormat == AudioFormat.Wav)
			{
				pcm = await ResampleWithFfmpegAsync(pcm, options, channels == 1, cancellationToken).ConfigureAwait(false);
				steps.Add(ConversionStep.Create(ConversionStepKind.Resampled, options.SampleRate));
			}

			PcmAudioTrack encoded = pcm;
			await Task.Run(() => new AcmEncoder().Encode(outputAcmPath, encoded, options, cancellationToken), cancellationToken).ConfigureAwait(false);
			steps.Add(ConversionStep.Create(ConversionStepKind.WroteAcm, pcm.SampleRate, channels));
			return new ConversionResult(inputAudioPath, outputAcmPath, steps);
		}

		public AcmInfo Inspect(String path, AcmDecodeOptions? options = null)
		{
			EnsureInputFile(path);
			return new AcmDecoder().Inspect(path, options);
		}

		private async Task<PcmAudioTrack> ReadSourcePcmAsync(String inputAudioPath, AudioFormat inputFormat, AcmEncodeOptions options, List<ConversionStep> steps, CancellationToken cancellationToken)
		{
			if (inputFormat == AudioFormat.Wav)
			{
				steps.Add(ConversionStep.Create(ConversionStepKind.ReadWav));
				return await Task.Run(() => new WavPcmReader().Read(inputAudioPath), cancellationToken).ConfigureAwait(false);
			}

			using TemporaryFile tempWav = new();
			await ffmpeg.DecodeToWavAsync(inputAudioPath, tempWav.Path, options.SampleRate, options.Channels == AcmChannelMode.Mono, cancellationToken).ConfigureAwait(false);
			steps.Add(ConversionStep.Create(ConversionStepKind.DecodedFfmpeg, inputFormat));
			return new WavPcmReader().Read(tempWav.Path);
		}

		private async Task<PcmAudioTrack> ResampleWithFfmpegAsync(PcmAudioTrack pcm, AcmEncodeOptions options, Boolean mono, CancellationToken cancellationToken)
		{
			using TemporaryFile tempIn = new("acmtools-in");
			using TemporaryFile tempOut = new("acmtools-out");
			new WavPcmWriter().Write(tempIn.Path, pcm);
			await ffmpeg.DecodeToWavAsync(tempIn.Path, tempOut.Path, options.SampleRate, mono, cancellationToken).ConfigureAwait(false);
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