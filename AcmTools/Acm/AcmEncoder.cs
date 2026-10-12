namespace AcmTools.Acm
{
	using System;
	using System.Collections.Concurrent;
	using System.IO;
	using System.Threading;
	using Audio;

	public sealed class AcmEncoder
	{
		private const Int32 MaxRows = 4095;
		private const Int32 FillerZero = 0;

		// A transform block holds about 2048 samples, like the original files (level 7 = 16 rows of 128 columns).
		private const Int32 TransformBlockSamples = 2048;

		// The signal fades to zero over this many columns after the end, so that the analysis, which assumes silence
		// after the last sample, stays exact up to the last real sample.
		private const Int32 TailColumns = 4;

		private const Int32 MaxStep = UInt16.MaxValue;
		private const Int32 MaxIndex = 32767;

		private static readonly ConcurrentDictionary<Int32, Double> NoiseGains = new();

		public void Encode(String path, PcmAudioTrack audio, AcmEncodeOptions? options = null, CancellationToken cancellationToken = default)
		{
			options ??= new AcmEncodeOptions();
			if (File.Exists(path) && !options.Overwrite)
			{
				throw new IOException($"Output file already exists: {path}");
			}

			PcmAudioTrack pcm = audio.To16Bit();
			Int32 channels = options.ResolveChannels(pcm.Channels, (Double)pcm.SampleCount / pcm.SampleRate);
			if (channels == 1)
			{
				pcm = pcm.ToMono16();
			}

			Int16[] samples = ReadSamples(pcm);
			if (samples.Length == 0)
			{
				throw new InvalidDataException("The source audio contains no samples.");
			}

			Int32 level = Math.Clamp(options.TransformLevel, 0, AcmEncodeOptions.MaxTransformLevel);
			Int32 maxBits = Math.Clamp(options.MaxBitsPerSample, AcmEncodeOptions.MinBitsPerSample, AcmEncodeOptions.LosslessBitsPerSample);

			// The format is one continuous bitstream: blocks are not byte-aligned.
			AcmBitWriter bits = new();
			if (level == 0)
			{
				EncodeLinear(bits, samples, pcm.SampleRate, channels, maxBits, cancellationToken);
			}
			else
			{
				EncodeTransformed(bits, samples, pcm.SampleRate, channels, level, options.TargetSnrDb, maxBits, cancellationToken);
			}

			String? directory = Path.GetDirectoryName(Path.GetFullPath(path));
			if (!String.IsNullOrWhiteSpace(directory))
			{
				Directory.CreateDirectory(directory);
			}

			File.WriteAllBytes(path, bits.ToArray());
		}

		private static void WriteHeader(AcmBitWriter bits, Int32 totalValues, Int32 sampleRate, Int32 channels, Int32 level, Int32 rows)
		{
			bits.WriteBits(0x032897, 24);
			bits.WriteBits(1, 8);
			bits.WriteBits(totalValues & 0xFFFF, 16);
			bits.WriteBits((totalValues >> 16) & 0xFFFF, 16);
			bits.WriteBits(channels, 16);
			bits.WriteBits(Math.Clamp(sampleRate, UInt16.MinValue, UInt16.MaxValue), 16);
			bits.WriteBits(level, 4);
			bits.WriteBits(rows, 12);
		}

		private static void EncodeLinear(AcmBitWriter bits, Int16[] samples, Int32 sampleRate, Int32 channels, Int32 maxBits, CancellationToken cancellationToken)
		{
			// The last block is padded to the block height, so use the fewest equally sized blocks.
			Int32 blockCount = (samples.Length + MaxRows - 1) / MaxRows;
			Int32 rows = (samples.Length + blockCount - 1) / blockCount;

			WriteHeader(bits, samples.Length, sampleRate, channels, 0, rows);
			for (Int32 block = 0; block < blockCount; block++)
			{
				cancellationToken.ThrowIfCancellationRequested();
				Int32 offset = block * rows;
				WriteLinearBlock(bits, samples, offset, Math.Min(rows, samples.Length - offset), rows, maxBits);
			}
		}

		private static void WriteLinearBlock(AcmBitWriter bits, Int16[] samples, Int32 offset, Int32 count, Int32 rows, Int32 maxBits)
		{
			Int32 min = 0;
			Int32 max = 0;
			for (Int32 i = 0; i < count; i++)
			{
				min = Math.Min(min, samples[offset + i]);
				max = Math.Max(max, samples[offset + i]);
			}

			if (min == 0 && max == 0)
			{
				bits.WriteBits(0, 4);
				bits.WriteBits(1, 16);
				bits.WriteBits(FillerZero, 5);
				return;
			}

			Int32 width = RequiredBits(min, max);
			Int32 step = 1;
			if (width > maxBits)
			{
				width = maxBits;
				Int32 largestIndex = (1 << (width - 1)) - 1;
				step = Math.Max(1, (Math.Max(max, -min) + largestIndex - 1) / largestIndex);
			}

			// A linear filler of N bits addresses indices in [-2^(N-1), 2^(N-1) - 1], which needs 2^(N-1) table entries.
			Int32 middle = 1 << (width - 1);
			bits.WriteBits(width - 1, 4);
			bits.WriteBits(step, 16);
			bits.WriteBits(width, 5);

			for (Int32 i = 0; i < count; i++)
			{
				Int32 index = Math.Clamp(DivideRound(samples[offset + i], step), -middle, middle - 1);
				bits.WriteBits(index + middle, width);
			}

			for (Int32 i = count; i < rows; i++)
			{
				bits.WriteBits(middle, width);
			}
		}

		/// <summary>
		/// Same structure as the original Interplay files: the audio goes through the sub-band transform, each block
		/// gets one quantization step, and every column of coefficients is stored with the cheapest filler.
		/// </summary>
		private static void EncodeTransformed(AcmBitWriter bits, Int16[] samples, Int32 sampleRate, Int32 channels, Int32 level, Double targetSnrDb, Int32 maxBits, CancellationToken cancellationToken)
		{
			Int32 columns = 1 << level;
			Int32 rows = Math.Max(1, TransformBlockSamples >> level);
			Int32 blockSize = rows * columns;
			Int32 tail = TailColumns * columns;
			Int32 blockCount = (samples.Length + tail + blockSize - 1) / blockSize;
			Double scale = 1 << level;

			Double[] coefficients = new Double[blockCount * blockSize];
			for (Int32 n = 0; n < samples.Length; n++)
			{
				coefficients[n] = samples[n] * scale;
			}

			// Stereo is stored interleaved, so each channel fades out from its own last sample.
			for (Int32 k = 0; k < tail; k++)
			{
				Int32 n = samples.Length + k;
				Double last = samples[Math.Max(0, n - ((k / channels) + 1) * channels)] * scale;
				coefficients[n] = last * 0.5 * (1 + Math.Cos(Math.PI * (k + 1) / (tail + 1)));
			}

			cancellationToken.ThrowIfCancellationRequested();
			// The decoder adds 1 (about 1/128 of a sample step) after its first stage; ignoring it keeps digital silence exactly silent.
			AcmTransform.Analyze(coefficients, level, false);

			Double noiseGain = NoiseGains.GetOrAdd(level, AcmTransform.NoiseGain);
			Double noiseRatio = Math.Pow(10, -targetSnrDb / 10);
			Int32 largestIndex = Math.Min((1 << (maxBits - 1)) - 1, MaxIndex);

			WriteHeader(bits, samples.Length, sampleRate, channels, level, rows);
			Int32[] indices = new Int32[blockSize];
			Int32[] column = new Int32[rows];
			for (Int32 block = 0; block < blockCount; block++)
			{
				cancellationToken.ThrowIfCancellationRequested();
				Int32 offset = block * blockSize;

				// Step from the noise the block may carry: Δ²/12 per coefficient, spread by the synthesis filters.
				Double energy = 0;
				Int32 real = Math.Max(0, Math.Min(blockSize, samples.Length - offset));
				for (Int32 i = 0; i < real; i++)
				{
					energy += (Double)samples[offset + i] * samples[offset + i];
				}

				Double noiseVariance = real == 0 ? 0 : energy / real * noiseRatio;
				Double peak = 0;
				for (Int32 i = 0; i < blockSize; i++)
				{
					peak = Math.Max(peak, Math.Abs(coefficients[offset + i]));
				}

				Double step = Math.Max(1, Math.Round(Math.Sqrt(12 * noiseVariance / noiseGain)));
				step = Math.Min(MaxStep, Math.Max(step, Math.Ceiling(peak / largestIndex)));

				Int32 minIndex = 0;
				Int32 maxIndex = 0;
				for (Int32 i = 0; i < blockSize; i++)
				{
					Double magnitude = Math.Round(Math.Abs(coefficients[offset + i]) / step, MidpointRounding.AwayFromZero);
					Int32 index = (Int32)Math.Min(magnitude, MaxIndex);
					indices[i] = coefficients[offset + i] < 0 ? -index : index;
					minIndex = Math.Min(minIndex, indices[i]);
					maxIndex = Math.Max(maxIndex, indices[i]);
				}

				// The block's value table holds indices from -2^power to 2^power - 1.
				Int32 power = 0;
				while (power < 15 && (maxIndex > (1 << power) - 1 || minIndex < -(1 << power)))
				{
					power++;
				}

				bits.WriteBits(power, 4);
				bits.WriteBits((Int32)step, 16);
				for (Int32 col = 0; col < columns; col++)
				{
					for (Int32 row = 0; row < rows; row++)
					{
						column[row] = indices[row * columns + col];
					}

					(Int32 filler, _) = AcmFillerCoder.Choose(column);
					bits.WriteBits(filler, 5);
					AcmFillerCoder.Write(bits, filler, column);
				}
			}
		}

		/// <summary>Smallest linear filler width (3 to 16 bits) able to store every value of [min, max] as is.</summary>
		private static Int32 RequiredBits(Int32 min, Int32 max)
		{
			for (Int32 width = AcmEncodeOptions.MinBitsPerSample; width < AcmEncodeOptions.LosslessBitsPerSample; width++)
			{
				Int32 middle = 1 << (width - 1);
				if (max <= middle - 1 && min >= -middle)
				{
					return width;
				}
			}

			return AcmEncodeOptions.LosslessBitsPerSample;
		}

		private static Int16[] ReadSamples(PcmAudioTrack pcm)
		{
			Byte[] bytes = pcm.To16Bit().Samples;
			Int16[] samples = new Int16[bytes.Length / 2];
			for (Int32 i = 0; i < samples.Length; i++)
			{
				samples[i] = (Int16)(bytes[i * 2] | (bytes[i * 2 + 1] << 8));
			}

			return samples;
		}

		private static Int32 DivideRound(Int32 value, Int32 divisor)
		{
			return value >= 0
				? (value + divisor / 2) / divisor
				: -((-value + divisor / 2) / divisor);
		}
	}
}