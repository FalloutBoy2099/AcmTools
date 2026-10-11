namespace AcmTools.Acm
{
	using System;
	using System.Collections.Generic;

	internal sealed class AcmBitWriter
	{
		private readonly List<Byte> bytes = [];
		private UInt32 bitData;
		private Int32 bitCount;

		public void WriteBits(Int32 value, Int32 count)
		{
			if (count < 0 || count > 31)
			{
				throw new ArgumentOutOfRangeException(nameof(count));
			}

			UInt32 mask = count == 32 ? UInt32.MaxValue : ((UInt32)1 << count) - 1;
			bitData |= ((UInt32)value & mask) << bitCount;
			bitCount += count;
			while (bitCount >= 8)
			{
				bytes.Add((Byte)(bitData & 0xFF));
				bitData >>= 8;
				bitCount -= 8;
			}
		}

		public Byte[] ToArray()
		{
			if (bitCount > 0)
			{
				bytes.Add((Byte)(bitData & 0xFF));
				bitData = 0;
				bitCount = 0;
			}

			return [.. bytes];
		}
	}
}