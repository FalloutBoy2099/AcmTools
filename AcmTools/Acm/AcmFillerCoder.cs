namespace AcmTools.Acm
{
	using System;

	/// <summary>Writes one column of quantized indices with the cheapest of the ACM "fillers".</summary>
	internal static class AcmFillerCoder
	{
		public const Int32 Zero = 0;
		public const Int32 LinearMin = 3;
		public const Int32 LinearMax = 16;

		// Fillers with a small alphabet, and the largest absolute index each can express.
		private static readonly (Int32 Code, Int32 Limit)[] SmallFillers =
		[
			(17, 1), (18, 1), (19, 1),
			(20, 2), (21, 2), (22, 2),
			(23, 3), (24, 3),
			(26, 4), (27, 4),
			(29, 5)
		];

		/// <summary>Cheapest filler for the values, with its size in bits.</summary>
		public static (Int32 Filler, Int32 Bits) Choose(ReadOnlySpan<Int32> values)
		{
			Int32 min = 0;
			Int32 max = 0;
			foreach (Int32 value in values)
			{
				min = Math.Min(min, value);
				max = Math.Max(max, value);
			}

			if (min == 0 && max == 0)
			{
				return (Zero, 0);
			}

			Int32 width = LinearMin;
			while (width < LinearMax && (max > (1 << (width - 1)) - 1 || min < -(1 << (width - 1))))
			{
				width++;
			}

			Int32 bestFiller = width;
			Int32 bestBits = width * values.Length;
			Int32 largest = Math.Max(max, -min);
			foreach ((Int32 code, Int32 limit) in SmallFillers)
			{
				if (largest > limit)
				{
					continue;
				}

				Int32 bits = 0;
				Write(code, values, (_, count) => bits += count);
				if (bits < bestBits)
				{
					bestFiller = code;
					bestBits = bits;
				}
			}

			return (bestFiller, bestBits);
		}

		public static void Write(AcmBitWriter bits, Int32 filler, ReadOnlySpan<Int32> values)
		{
			Write(filler, values, bits.WriteBits);
		}

		private static void Write(Int32 filler, ReadOnlySpan<Int32> values, Action<Int32, Int32> put)
		{
			switch (filler)
			{
				case Zero:
					return;
				case >= LinearMin and <= LinearMax:
					Int32 middle = 1 << (filler - 1);
					foreach (Int32 value in values)
					{
						put(value + middle, filler);
					}

					return;
				case 17:
					WriteZeroRun(values, put, value => Map1(value, put));
					return;
				case 20:
					WriteZeroRun(values, put, value => Map2Near(value, put));
					return;
				case 23:
					WriteZeroRun(values, put, value => Map1Or2Far(value, put));
					return;
				case 26:
					WriteZeroRun(values, put, value => Map3(value, put));
					return;
				case 18:
					WriteEscaped(values, put, value => Map1(value, put));
					return;
				case 21:
					WriteEscaped(values, put, value => Map2Near(value, put));
					return;
				case 24:
					WriteEscaped(values, put, value => Map1Or2Far(value, put));
					return;
				case 27:
					WriteEscaped(values, put, value => Map3(value, put));
					return;
				case 19:
					WritePacked(values, put, 3, 1, 5, 3);
					return;
				case 22:
					WritePacked(values, put, 3, 2, 7, 5);
					return;
				case 29:
					WritePacked(values, put, 2, 5, 7, 11);
					return;
				default:
					throw new ArgumentOutOfRangeException(nameof(filler));
			}
		}

		// 0 = two zero rows, 10 = one zero row, 11 + payload = a non-zero row.
		private static void WriteZeroRun(ReadOnlySpan<Int32> values, Action<Int32, Int32> put, Action<Int32> writePayload)
		{
			for (Int32 i = 0; i < values.Length; i++)
			{
				if (values[i] != 0)
				{
					put(1, 1);
					put(1, 1);
					writePayload(values[i]);
				}
				else if (i + 1 < values.Length && values[i + 1] == 0)
				{
					put(0, 1);
					i++;
				}
				else
				{
					put(1, 1);
					put(0, 1);
				}
			}
		}

		// 0 = zero row, 1 + payload = a non-zero row.
		private static void WriteEscaped(ReadOnlySpan<Int32> values, Action<Int32, Int32> put, Action<Int32> writePayload)
		{
			foreach (Int32 value in values)
			{
				if (value == 0)
				{
					put(0, 1);
				}
				else
				{
					put(1, 1);
					writePayload(value);
				}
			}
		}

		// Several small values packed in one base-N symbol, the first row in the lowest digit.
		private static void WritePacked(ReadOnlySpan<Int32> values, Action<Int32, Int32> put, Int32 perSymbol, Int32 offset, Int32 symbolBits, Int32 radix)
		{
			for (Int32 i = 0; i < values.Length; i += perSymbol)
			{
				Int32 symbol = 0;
				Int32 weight = 1;
				for (Int32 j = 0; j < perSymbol; j++)
				{
					Int32 value = i + j < values.Length ? values[i + j] : 0;
					symbol += (value + offset) * weight;
					weight *= radix;
				}

				put(symbol, symbolBits);
			}
		}

		private static void Map1(Int32 value, Action<Int32, Int32> put)
		{
			put(value > 0 ? 1 : 0, 1);
		}

		private static void Map2Near(Int32 value, Action<Int32, Int32> put)
		{
			put(value < 0 ? value + 2 : value + 1, 2);
		}

		private static void Map3(Int32 value, Action<Int32, Int32> put)
		{
			put(value < 0 ? value + 4 : value + 3, 3);
		}

		// 0 + one bit for +-1, 1 + two bits for +-2 / +-3.
		private static void Map1Or2Far(Int32 value, Action<Int32, Int32> put)
		{
			if (Math.Abs(value) == 1)
			{
				put(0, 1);
				Map1(value, put);
			}
			else
			{
				put(1, 1);
				put(value < 0 ? value + 3 : value, 2);
			}
		}
	}
}