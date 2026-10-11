namespace AcmTools.Acm
{
	using System;
	using System.IO;
	using Audio;

	public sealed class AcmEncoder
	{
		private const Int32 MaxRows = 4095;
		private const Int32 LinearFillerBits = 15;
		private const Int32 LinearFillerCode = 15;
		private const Int32 LinearMiddle = 1 << (LinearFillerBits - 1);

		public void Encode(String path, PcmAudioTrack audio, AcmEncodeOptions? options = null)
		{
			options ??= new AcmEncodeOptions();
			if (File.Exists(path) && !options.Overwrite)
			{
				throw new IOException($"Output file already exists: {path}");
			}

			PcmAudioTrack pcm = options.ForceMono ? audio.ToMono16() : audio.To16Bit();
			if (pcm.Channels != 1)
			{
				throw new NotSupportedException("ACM encoding currently writes mono files. Use ForceMono or provide mono PCM.");
			}

			String? directory = Path.GetDirectoryName(Path.GetFullPath(path));
			if (!String.IsNullOrWhiteSpace(directory))
			{
				Directory.CreateDirectory(directory);
			}

			Int16[] samples = ReadSamples(pcm);
			using FileStream stream = File.Create(path);
			using BinaryWriter writer = new(stream);
			WriteHeader(writer, samples.Length, pcm.SampleRate);

			// Blocks are not byte-aligned in the ACM format: the whole file is one continuous bitstream.
			AcmBitWriter bits = new();
			Int32 offset = 0;
			while (offset < samples.Length)
			{
				Int32 count = Math.Min(MaxRows, samples.Length - offset);
				WriteLinearBlock(bits, samples, offset, count);
				offset += count;
			}

			writer.Write(bits.ToArray());
		}

		private static void WriteHeader(BinaryWriter writer, Int32 totalValues, Int32 sampleRate)
		{
			writer.Write((Byte)0x97);
			writer.Write((Byte)0x28);
			writer.Write((Byte)0x03);
			writer.Write((Byte)0x01);
			writer.Write((UInt32)totalValues);
			writer.Write((UInt16)1);
			writer.Write((UInt16)Math.Clamp(sampleRate, UInt16.MinValue, UInt16.MaxValue));
			writer.Write((UInt16)(MaxRows << 4));
		}

		private static void WriteLinearBlock(AcmBitWriter bits, Int16[] samples, Int32 offset, Int32 count)
		{
			Int32 maxAbs = 0;
			for (Int32 i = 0; i < count; i++)
			{
				maxAbs = Math.Max(maxAbs, Math.Abs((Int32)samples[offset + i]));
			}

			Int32 step = Math.Max(1, (maxAbs + LinearMiddle - 2) / (LinearMiddle - 1));
			step = Math.Clamp(step, 1, UInt16.MaxValue);

			bits.WriteBits(15, 4);
			bits.WriteBits(step, 16);
			bits.WriteBits(LinearFillerCode, 5);

			for (Int32 i = 0; i < count; i++)
			{
				Int32 scaled = DivideRound(samples[offset + i], step);
				scaled = Math.Clamp(scaled, -LinearMiddle, LinearMiddle - 1);
				bits.WriteBits(scaled + LinearMiddle, LinearFillerBits);
			}

			for (Int32 i = count; i < MaxRows; i++)
			{
				bits.WriteBits(LinearMiddle, LinearFillerBits);
			}
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