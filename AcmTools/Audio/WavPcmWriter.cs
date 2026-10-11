namespace AcmTools.Audio
{
	using System;
	using System.IO;
	using IO;

	public sealed class WavPcmWriter
	{
		public void Write(String path, PcmAudioTrack audio)
		{
			PcmAudioTrack pcm = audio.To16Bit();
			String? directory = Path.GetDirectoryName(Path.GetFullPath(path));
			if (!String.IsNullOrWhiteSpace(directory))
			{
				Directory.CreateDirectory(directory);
			}

			using FileStream stream = File.Create(path);
			using BinaryWriter writer = new(stream);
			writer.WriteAscii("RIFF");
			writer.Write(36 + pcm.Samples.Length);
			writer.WriteAscii("WAVE");
			writer.WriteAscii("fmt ");
			writer.Write(16);
			writer.Write((UInt16)1);
			writer.Write((UInt16)pcm.Channels);
			writer.Write((UInt32)pcm.SampleRate);
			writer.Write((UInt32)(pcm.SampleRate * pcm.BlockAlign));
			writer.Write((UInt16)pcm.BlockAlign);
			writer.Write((UInt16)pcm.BitsPerSample);
			writer.WriteAscii("data");
			writer.Write(pcm.Samples.Length);
			writer.Write(pcm.Samples);
		}
	}
}