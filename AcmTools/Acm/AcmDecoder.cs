namespace AcmTools.Acm
{
	using System;
	using System.Collections.Generic;
	using System.IO;
	using Audio;

	public sealed class AcmDecoder
	{
		private const Int32 AcmId = 0x032897;
		private const Int32 WavcId = 0x564157;

		private static readonly Int32[] Map1Bit = [-1, +1];
		private static readonly Int32[] Map2BitNear = [-2, -1, +1, +2];
		private static readonly Int32[] Map2BitFar = [-3, -2, +2, +3];
		private static readonly Int32[] Map3Bit = [-4, -3, -2, -1, +1, +2, +3, +4];
		private static readonly Int32[] Mul3X3 = GenerateMul3X3();
		private static readonly Int32[] Mul3X5 = GenerateMul3X5();
		private static readonly Int32[] Mul2X11 = GenerateMul2X11();

		public PcmAudioTrack Decode(String path, AcmDecodeOptions? options = null)
		{
			options ??= new AcmDecodeOptions();
			Byte[] bytes = File.ReadAllBytes(path);
			DecoderState state = ReadHeader(bytes, options);
			List<Byte> pcm = new(state.TotalValues * 2);

			while (state.StreamPosition < state.TotalValues)
			{
				if (!TryDecodeBlock(state))
				{
					break;
				}

				Int32 available = Math.Min(state.Block.Length, state.TotalValues - state.StreamPosition);
				if (state.Channels > 1)
				{
					available -= available % state.Channels;
				}

				for (Int32 i = 0; i < available; i++)
				{
					Int32 value = state.Block[i] >> state.Level;
					Int16 sample = (Int16)Math.Clamp(value, Int16.MinValue, Int16.MaxValue);
					pcm.Add((Byte)(sample & 0xFF));
					pcm.Add((Byte)((sample >> 8) & 0xFF));
				}

				state.StreamPosition += available;
			}

			Int32 expectedBytes = state.TotalValues * 2;
			while (pcm.Count < expectedBytes)
			{
				pcm.Add(0);
				pcm.Add(0);
			}

			return new PcmAudioTrack(state.Rate, (Int16)state.Channels, 16, [.. pcm]);
		}

		public AcmInfo Inspect(String path, AcmDecodeOptions? options = null)
		{
			DecoderState state = ReadHeader(File.ReadAllBytes(path), options ?? new AcmDecodeOptions());
			return new AcmInfo(
				state.TotalValues / state.Channels,
				(Int16)state.HeaderChannels,
				(Int16)state.Channels,
				state.Rate,
				state.Level,
				state.Rows);
		}

		private static DecoderState ReadHeader(Byte[] bytes, AcmDecodeOptions options)
		{
			AcmBitReader reader = new(bytes);
			Boolean wavc = false;
			Int32 tmp = reader.ReadBits(24);
			if (tmp == WavcId)
			{
				if (reader.ReadBits(8) != 'C')
				{
					throw new InvalidDataException("The file is not an ACM stream.");
				}

				Int32 v0 = reader.ReadBits(16);
				Int32 v1 = reader.ReadBits(16);
				for (Int32 i = 2; i < 12; i++)
				{
					Int32 value = reader.ReadBits(16);
					if (i == 6 && value != 28)
					{
						throw new InvalidDataException("The WAVC wrapper has an unsupported header length.");
					}
				}

				if (v0 != 0x3156 || v1 != 0x302E)
				{
					throw new InvalidDataException("The WAVC wrapper version is unsupported.");
				}

				wavc = true;
				tmp = reader.ReadBits(24);
			}

			if (tmp != AcmId || reader.ReadBits(8) != 1)
			{
				throw new InvalidDataException("The file is not an ACM stream.");
			}

			Int32 totalValues = reader.ReadBits(16) | (reader.ReadBits(16) << 16);
			Int32 headerChannels = reader.ReadBits(16);
			Int32 channels = options.ForceFalloutMono ? 1 : headerChannels;
			Int32 rate = reader.ReadBits(16);
			Int32 level = reader.ReadBits(4);
			Int32 rows = reader.ReadBits(12);

			if (totalValues <= 0 || headerChannels is < 1 or > 2 || channels is < 1 or > 2 || rate < 4096 || rows <= 0)
			{
				throw new InvalidDataException("The ACM header contains invalid values.");
			}

			if (!options.ForceFalloutMono && !wavc && channels < 2)
			{
				channels = 2;
			}

			Int32 cols = 1 << level;
			return new DecoderState(reader, totalValues, headerChannels, channels, rate, level, rows, cols);
		}

		private static Boolean TryDecodeBlock(DecoderState state)
		{
			if (!state.Reader.TryReadBits(4, out Int32 power) || !state.Reader.TryReadBits(16, out Int32 step))
			{
				return false;
			}

			Int32 count = 1 << power;
			for (Int32 i = 0, x = 0; i < count; i++, x += step)
			{
				state.MidBuffer[32768 + i] = x;
			}

			for (Int32 i = 1, x = -step; i <= count; i++, x -= step)
			{
				state.MidBuffer[32768 - i] = x;
			}

			for (Int32 col = 0; col < state.Cols; col++)
			{
				if (!state.Reader.TryReadBits(5, out Int32 filler))
				{
					return false;
				}

				FillColumn(state, filler, col);
			}

			JuggleBlock(state);
			return true;
		}

		private static void FillColumn(DecoderState state, Int32 filler, Int32 col)
		{
			switch (filler)
			{
				case 0:
					for (Int32 i = 0; i < state.Rows; i++)
					{
						SetPos(state, i, col, 0);
					}

					return;
				case >= 3 and <= 16:
					FillLinear(state, filler, col);
					return;
				case 17:
					FillK13(state, col);
					return;
				case 18:
					FillK12(state, col);
					return;
				case 19:
					FillT15(state, col);
					return;
				case 20:
					FillK24(state, col);
					return;
				case 21:
					FillK23(state, col);
					return;
				case 22:
					FillT27(state, col);
					return;
				case 23:
					FillK35(state, col);
					return;
				case 24:
					FillK34(state, col);
					return;
				case 26:
					FillK45(state, col);
					return;
				case 27:
					FillK44(state, col);
					return;
				case 29:
					FillT37(state, col);
					return;
				default:
					throw new InvalidDataException("The ACM stream uses a corrupt filler code.");
			}
		}

		private static void FillLinear(DecoderState state, Int32 bits, Int32 col)
		{
			Int32 middle = 1 << (bits - 1);
			for (Int32 i = 0; i < state.Rows; i++)
			{
				SetPos(state, i, col, state.Reader.ReadBits(bits) - middle);
			}
		}

		private static void FillK13(DecoderState state, Int32 col)
		{
			for (Int32 i = 0; i < state.Rows; i++)
			{
				if (state.Reader.ReadBits(1) == 0)
				{
					SetPos(state, i++, col, 0);
					if (i < state.Rows)
					{
						SetPos(state, i, col, 0);
					}
				}
				else if (state.Reader.ReadBits(1) == 0)
				{
					SetPos(state, i, col, 0);
				}
				else
				{
					SetPos(state, i, col, Map1Bit[state.Reader.ReadBits(1)]);
				}
			}
		}

		private static void FillK12(DecoderState state, Int32 col)
		{
			for (Int32 i = 0; i < state.Rows; i++)
			{
				SetPos(state, i, col, state.Reader.ReadBits(1) == 0 ? 0 : Map1Bit[state.Reader.ReadBits(1)]);
			}
		}

		private static void FillK24(DecoderState state, Int32 col)
		{
			for (Int32 i = 0; i < state.Rows; i++)
			{
				if (state.Reader.ReadBits(1) == 0)
				{
					SetPos(state, i++, col, 0);
					if (i < state.Rows)
					{
						SetPos(state, i, col, 0);
					}
				}
				else if (state.Reader.ReadBits(1) == 0)
				{
					SetPos(state, i, col, 0);
				}
				else
				{
					SetPos(state, i, col, Map2BitNear[state.Reader.ReadBits(2)]);
				}
			}
		}

		private static void FillK23(DecoderState state, Int32 col)
		{
			for (Int32 i = 0; i < state.Rows; i++)
			{
				SetPos(state, i, col, state.Reader.ReadBits(1) == 0 ? 0 : Map2BitNear[state.Reader.ReadBits(2)]);
			}
		}

		private static void FillK35(DecoderState state, Int32 col)
		{
			for (Int32 i = 0; i < state.Rows; i++)
			{
				if (state.Reader.ReadBits(1) == 0)
				{
					SetPos(state, i++, col, 0);
					if (i < state.Rows)
					{
						SetPos(state, i, col, 0);
					}
				}
				else if (state.Reader.ReadBits(1) == 0)
				{
					SetPos(state, i, col, 0);
				}
				else if (state.Reader.ReadBits(1) == 0)
				{
					SetPos(state, i, col, Map1Bit[state.Reader.ReadBits(1)]);
				}
				else
				{
					SetPos(state, i, col, Map2BitFar[state.Reader.ReadBits(2)]);
				}
			}
		}

		private static void FillK34(DecoderState state, Int32 col)
		{
			for (Int32 i = 0; i < state.Rows; i++)
			{
				if (state.Reader.ReadBits(1) == 0)
				{
					SetPos(state, i, col, 0);
				}
				else if (state.Reader.ReadBits(1) == 0)
				{
					SetPos(state, i, col, Map1Bit[state.Reader.ReadBits(1)]);
				}
				else
				{
					SetPos(state, i, col, Map2BitFar[state.Reader.ReadBits(2)]);
				}
			}
		}

		private static void FillK45(DecoderState state, Int32 col)
		{
			for (Int32 i = 0; i < state.Rows; i++)
			{
				if (state.Reader.ReadBits(1) == 0)
				{
					SetPos(state, i++, col, 0);
					if (i < state.Rows)
					{
						SetPos(state, i, col, 0);
					}
				}
				else if (state.Reader.ReadBits(1) == 0)
				{
					SetPos(state, i, col, 0);
				}
				else
				{
					SetPos(state, i, col, Map3Bit[state.Reader.ReadBits(3)]);
				}
			}
		}

		private static void FillK44(DecoderState state, Int32 col)
		{
			for (Int32 i = 0; i < state.Rows; i++)
			{
				SetPos(state, i, col, state.Reader.ReadBits(1) == 0 ? 0 : Map3Bit[state.Reader.ReadBits(3)]);
			}
		}

		private static void FillT15(DecoderState state, Int32 col)
		{
			for (Int32 i = 0; i < state.Rows; i++)
			{
				Int32 packed = Mul3X3[state.Reader.ReadBits(5)];
				SetPos(state, i++, col, (packed & 0x0F) - 1);
				if (i >= state.Rows) break;
				SetPos(state, i++, col, ((packed >> 4) & 0x0F) - 1);
				if (i >= state.Rows) break;
				SetPos(state, i, col, ((packed >> 8) & 0x0F) - 1);
			}
		}

		private static void FillT27(DecoderState state, Int32 col)
		{
			for (Int32 i = 0; i < state.Rows; i++)
			{
				Int32 packed = Mul3X5[state.Reader.ReadBits(7)];
				SetPos(state, i++, col, (packed & 0x0F) - 2);
				if (i >= state.Rows) break;
				SetPos(state, i++, col, ((packed >> 4) & 0x0F) - 2);
				if (i >= state.Rows) break;
				SetPos(state, i, col, ((packed >> 8) & 0x0F) - 2);
			}
		}

		private static void FillT37(DecoderState state, Int32 col)
		{
			for (Int32 i = 0; i < state.Rows; i++)
			{
				Int32 packed = Mul2X11[state.Reader.ReadBits(7)];
				SetPos(state, i++, col, (packed & 0x0F) - 5);
				if (i >= state.Rows) break;
				SetPos(state, i, col, ((packed >> 4) & 0x0F) - 5);
			}
		}

		private static void SetPos(DecoderState state, Int32 row, Int32 col, Int32 index)
		{
			state.Block[(row << state.Level) + col] = state.MidBuffer[32768 + index];
		}

		private static void JuggleBlock(DecoderState state)
		{
			if (state.Level == 0)
			{
				return;
			}

			Int32 stepSubcount = state.Level > 9 ? 1 : (2048 >> state.Level) - 2;
			Int32 todoCount = state.Rows;
			Int32 blockOffset = 0;
			while (true)
			{
				Int32 wrapOffset = 0;
				Int32 subCount = Math.Min(stepSubcount, todoCount) * 2;
				Int32 subLen = state.Cols / 2;
				Juggle(state.WrapBuffer, wrapOffset, state.Block, blockOffset, subLen, subCount);
				wrapOffset += subLen * 2;

				for (Int32 i = 0, p = blockOffset; i < subCount; i++, p += subLen)
				{
					state.Block[p]++;
				}

				while (subLen > 1)
				{
					subLen /= 2;
					subCount *= 2;
					Juggle(state.WrapBuffer, wrapOffset, state.Block, blockOffset, subLen, subCount);
					wrapOffset += subLen * 2;
				}

				if (todoCount <= stepSubcount)
				{
					break;
				}

				todoCount -= stepSubcount;
				blockOffset += stepSubcount << state.Level;
			}
		}

		private static void Juggle(Int32[] wrap, Int32 wrapOffset, Int32[] block, Int32 blockOffset, Int32 subLen, Int32 subCount)
		{
			for (Int32 i = 0; i < subLen; i++)
			{
				Int32 p = blockOffset + i;
				Int32 r0 = wrap[wrapOffset++];
				Int32 r1 = wrap[wrapOffset++];
				for (Int32 j = 0; j < subCount / 2; j++)
				{
					Int32 r2 = block[p];
					block[p] = r1 * 2 + (r0 + r2);
					p += subLen;
					Int32 r3 = block[p];
					block[p] = r2 * 2 - (r1 + r3);
					p += subLen;
					r0 = r2;
					r1 = r3;
				}

				wrap[wrapOffset - 2] = r0;
				wrap[wrapOffset - 1] = r1;
			}
		}

		private static Int32[] GenerateMul3X3()
		{
			Int32[] result = new Int32[27];
			for (Int32 x3 = 0; x3 < 3; x3++)
			for (Int32 x2 = 0; x2 < 3; x2++)
			for (Int32 x1 = 0; x1 < 3; x1++)
			{
				result[x1 + x2 * 3 + x3 * 9] = x1 + (x2 << 4) + (x3 << 8);
			}

			return result;
		}

		private static Int32[] GenerateMul3X5()
		{
			Int32[] result = new Int32[125];
			for (Int32 x3 = 0; x3 < 5; x3++)
			for (Int32 x2 = 0; x2 < 5; x2++)
			for (Int32 x1 = 0; x1 < 5; x1++)
			{
				result[x1 + x2 * 5 + x3 * 25] = x1 + (x2 << 4) + (x3 << 8);
			}

			return result;
		}

		private static Int32[] GenerateMul2X11()
		{
			Int32[] result = new Int32[121];
			for (Int32 x2 = 0; x2 < 11; x2++)
			for (Int32 x1 = 0; x1 < 11; x1++)
			{
				result[x1 + x2 * 11] = x1 + (x2 << 4);
			}

			return result;
		}

		private sealed class DecoderState
		{
			public DecoderState(AcmBitReader reader, Int32 totalValues, Int32 headerChannels, Int32 channels, Int32 rate, Int32 level, Int32 rows, Int32 cols)
			{
				Reader = reader;
				TotalValues = totalValues;
				HeaderChannels = headerChannels;
				Channels = channels;
				Rate = rate;
				Level = level;
				Rows = rows;
				Cols = cols;
				Block = new Int32[rows * cols];
				WrapBuffer = new Int32[Math.Max(0, 2 * cols - 2)];
				MidBuffer = new Int32[65536];
			}

			public AcmBitReader Reader { get; }

			public Int32 TotalValues { get; }

			public Int32 HeaderChannels { get; }

			public Int32 Channels { get; }

			public Int32 Rate { get; }

			public Int32 Level { get; }

			public Int32 Rows { get; }

			public Int32 Cols { get; }

			public Int32[] Block { get; }

			public Int32[] WrapBuffer { get; }

			public Int32[] MidBuffer { get; }

			public Int32 StreamPosition { get; set; }
		}
	}
}