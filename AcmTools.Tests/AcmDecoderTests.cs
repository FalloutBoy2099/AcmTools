namespace AcmTools.Tests
{
	using System;
	using System.IO;
	using AcmTools.Acm;
	using AcmTools.Audio;
	using AcmTools.Conversion;

	public sealed class AcmDecoderTests
	{
		[Fact]
		public void InspectReadsFalloutHeaderAsMonoByDefault()
		{
			String path = @"C:\Fallout2\F2_RPU_2.4.34_old\master.dat\sound\sfx\ammo.acm";
			if (!File.Exists(path))
			{
				return;
			}

			AcmInfo info = new AcmDecoder().Inspect(path);

			Assert.True(info.SampleCount > 0);
			Assert.Equal(1, info.Channels);
			Assert.True(info.SampleRate >= 4096);
		}

		[Fact]
		public void EncodeDecodeRoundTripsMonoPcmLength()
		{
			String directory = Path.Combine(Path.GetTempPath(), $"acmtools-test-{Guid.NewGuid():N}");
			Directory.CreateDirectory(directory);
			try
			{
				String acmPath = Path.Combine(directory, "tone.acm");
				Byte[] bytes = new Byte[22050 * 2 / 2];
				for (Int32 i = 0; i < bytes.Length / 2; i++)
				{
					Int16 sample = (Int16)(Math.Sin(i / 12.0) * 12000);
					bytes[i * 2] = (Byte)(sample & 0xFF);
					bytes[i * 2 + 1] = (Byte)((sample >> 8) & 0xFF);
				}

				PcmAudioTrack source = new(22050, 1, 16, bytes);
				new AcmEncoder().Encode(acmPath, source);
				PcmAudioTrack decoded = new AcmDecoder().Decode(acmPath);

				Assert.Equal(source.SampleRate, decoded.SampleRate);
				Assert.Equal(1, decoded.Channels);
				Assert.Equal(source.Samples.Length, decoded.Samples.Length);
			}
			finally
			{
				if (Directory.Exists(directory))
				{
					Directory.Delete(directory, true);
				}
			}
		}

		[Theory]
		[InlineData(1)]
		[InlineData(4095)]
		[InlineData(4096)]
		[InlineData(4097)]
		[InlineData(11025)]
		[InlineData(22050)]
		[InlineData(40000)]
		public void EncodeDecodeRoundTripsSamplesAcrossBlocks(Int32 sampleCount)
		{
			String directory = Path.Combine(Path.GetTempPath(), $"acmtools-test-{Guid.NewGuid():N}");
			Directory.CreateDirectory(directory);
			try
			{
				String acmPath = Path.Combine(directory, "tone.acm");
				Byte[] bytes = new Byte[sampleCount * 2];
				for (Int32 i = 0; i < sampleCount; i++)
				{
					Int16 sample = (Int16)(Math.Sin(i / 12.0) * 12000);
					bytes[i * 2] = (Byte)(sample & 0xFF);
					bytes[i * 2 + 1] = (Byte)((sample >> 8) & 0xFF);
				}

				PcmAudioTrack source = new(22050, 1, 16, bytes);
				new AcmEncoder().Encode(acmPath, source);
				PcmAudioTrack decoded = new AcmDecoder().Decode(acmPath);

				Assert.Equal(source.Samples.Length, decoded.Samples.Length);
				for (Int32 i = 0; i < sampleCount; i++)
				{
					Int32 expected = (Int16)(source.Samples[i * 2] | (source.Samples[i * 2 + 1] << 8));
					Int32 actual = (Int16)(decoded.Samples[i * 2] | (decoded.Samples[i * 2 + 1] << 8));
					Assert.True(Math.Abs(expected - actual) <= 1, $"Sample {i}: expected {expected}, got {actual}.");
				}
			}
			finally
			{
				if (Directory.Exists(directory))
				{
					Directory.Delete(directory, true);
				}
			}
		}

		[Fact]
		public async Task ConverterWritesWavFromAcm()
		{
			String source = @"C:\Fallout2\F2_RPU_2.4.34_old\master.dat\sound\speech\narrator\nar_10.acm";
			if (!File.Exists(source))
			{
				return;
			}

			String directory = Path.Combine(Path.GetTempPath(), $"acmtools-test-{Guid.NewGuid():N}");
			Directory.CreateDirectory(directory);
			try
			{
				String output = Path.Combine(directory, "nar_10.wav");
				await new AcmConverter().ConvertAcmToAudioAsync(source, output);
				PcmAudioTrack wav = new WavPcmReader().Read(output);

				Assert.Equal(1, wav.Channels);
				Assert.True(wav.SampleCount > 1000);
			}
			finally
			{
				if (Directory.Exists(directory))
				{
					Directory.Delete(directory, true);
				}
			}
		}
	}
}
