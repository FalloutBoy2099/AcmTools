namespace AcmTools.IO
{
	using System;
	using System.IO;
	using System.Text;

	internal static class BinaryWriterExtensions
	{
		public static void WriteAscii(this BinaryWriter writer, String value)
		{
			writer.Write(Encoding.ASCII.GetBytes(value));
		}
	}
}