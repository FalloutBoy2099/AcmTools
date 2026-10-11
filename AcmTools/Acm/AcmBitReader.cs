namespace AcmTools.Acm
{
	using System;
	using System.IO;

	internal sealed class AcmBitReader
	{
		private readonly Byte[] data;
		private Int32 bytePosition;
		private UInt32 bitData;
		private Int32 bitAvailable;
		private Boolean eofPaddingUsed;

		public AcmBitReader(Byte[] data)
		{
			this.data = data;
		}

		public Boolean TryReadBits(Int32 count, out Int32 value)
		{
			if (count < 0 || count > 31)
			{
				throw new ArgumentOutOfRangeException(nameof(count));
			}

			while (bitAvailable < count)
			{
				if (bytePosition < data.Length)
				{
					bitData |= (UInt32)data[bytePosition++] << bitAvailable;
					bitAvailable += 8;
					continue;
				}

				if (!eofPaddingUsed)
				{
					eofPaddingUsed = true;
					bitAvailable += 8;
					continue;
				}

				value = 0;
				return false;
			}

			UInt32 mask = count == 32 ? UInt32.MaxValue : ((UInt32)1 << count) - 1;
			value = (Int32)(bitData & mask);
			bitData >>= count;
			bitAvailable -= count;
			return true;
		}

		public Int32 ReadBits(Int32 count)
		{
			if (!TryReadBits(count, out Int32 value))
			{
				throw new EndOfStreamException("Unexpected end of ACM bitstream.");
			}

			return value;
		}
	}
}