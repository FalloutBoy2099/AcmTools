namespace AcmTools.Acm
{
	using System;
	using System.IO;
	using System.Threading;
	using Audio;

	public sealed class AcmDecoder
	{
		private const Int32 AcmId = 0x032897;
		private const Int32 WavcId = 0x564157;
		private const Int32 MidBufferCenter = 32768;

		// Upper bound of decoded values (about 3 hours at 22 kHz); protects against corrupt headers requesting gigabytes.
		private const Int32 MaxTotalValues = 1 << 28;

		private const Int32 FillerZero = 0;
		private const Int32 FillerLinearMin = 3;
		private const Int32 FillerLinearMax = 16;
		private const Int32 FillerK13 = 17;
		private const Int32 FillerK12 = 18;
		private const Int32 FillerT15 = 19;
		private const Int32 FillerK24 = 20;
		private const Int32 FillerK23 = 21;
		private const Int32 FillerT27 = 22;
		private const Int32 FillerK35 = 23;
		private const Int32 FillerK34 = 24;
		private const Int32 FillerK45 = 26;
		private const Int32 FillerK44 = 27;
		private const Int32 FillerT37 = 29;

		private static readonly Int32[] Map1Bit = [-1, +1];
		private static readonly Int32[] Map2BitNear = [-2, -1, +1, +2];
		private static readonly Int32[] Map2BitFar = [-3, -2, +2, +3];
		private static readonly Int32[] Map3Bit = [-4, -3, -2, -1, +1, +2, +3, +4];

		public PcmAudioTrack Decode(String path, AcmDecodeOptions? options = null, CancellationToken cancellationToken = default)
		{
			options ??= new AcmDecodeOptions();
			Byte[] bytes = File.ReadAllBytes(path);
			DecoderState state = ReadHeader(bytes, options);
			Byte[] pcm = new Byte[state.TotalValues * 2];
			Int32 written = 0;

			while (state.StreamPosition < state.TotalValues)
			{
				cancellationToken.ThrowIfCancellationRequested();
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
					pcm[written++] = (Byte)(sample & 0xFF);
					pcm[written++] = (Byte)((sample >> 8) & 0xFF);
				}

				state.StreamPosition += available;
			}

			// A truncated stream is completed with silence (the array is already zeroed).
			return new PcmAudioTrack(state.Rate, (Int16)state.Channels, 16, pcm);
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
			Int32 rate = reader.ReadBits(16);
			Int32 level = reader.ReadBits(4);
			Int32 rows = reader.ReadBits(12);

			if (totalValues <= 0 || totalValues > MaxTotalValues || headerChannels is < 1 or > 2 || rate < 4096 || rows <= 0)
			{
				throw new InvalidDataException("The ACM header contains invalid values.");
			}

			// The channel count stored in plain ACM files is unreliable: most speech and sound effects are tagged stereo
			// although they are mono. In the game files only the music tracks (the sole files longer than a minute when
			// counted with the channels of the header) are really stereo.
			Boolean stereo = options.Channels switch
			{
				AcmChannelMode.Mono => false,
				AcmChannelMode.Stereo => true,
				_ => headerChannels == 2 && (Double)totalValues / headerChannels / rate > options.StereoThresholdSeconds
			};

			Int32 channels = stereo ? headerChannels : 1;

			// Same rule as the reference decoder (libacm) when stereo is requested explicitly: a plain ACM that claims
			// to be mono is read as stereo too, only the WAVC wrapper stores a trustworthy count.
			if (stereo && options.Channels == AcmChannelMode.Stereo && !wavc && channels < 2)
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
				state.MidBuffer[MidBufferCenter + i] = x;
			}

			for (Int32 i = 1, x = -step; i <= count; i++, x -= step)
			{
				state.MidBuffer[MidBufferCenter - i] = x;
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
				case FillerZero:
					for (Int32 i = 0; i < state.Rows; i++)
					{
						SetPos(state, i, col, 0);
					}

					return;
				case >= FillerLinearMin and <= FillerLinearMax:
					FillLinear(state, filler, col);
					return;
				case FillerK13:
					FillZeroRun(state, col, ReadMap1);
					return;
				case FillerK24:
					FillZeroRun(state, col, ReadMap2Near);
					return;
				case FillerK35:
					FillZeroRun(state, col, ReadMap1Or2Far);
					return;
				case FillerK45:
					FillZeroRun(state, col, ReadMap3);
					return;
				case FillerK12:
					FillEscaped(state, col, ReadMap1);
					return;
				case FillerK23:
					FillEscaped(state, col, ReadMap2Near);
					return;
				case FillerK34:
					FillEscaped(state, col, ReadMap1Or2Far);
					return;
				case FillerK44:
					FillEscaped(state, col, ReadMap3);
					return;
				case FillerT15:
					FillPacked(state, col, 3, 5, 3, 1);
					return;
				case FillerT27:
					FillPacked(state, col, 3, 7, 5, 2);
					return;
				case FillerT37:
					FillPacked(state, col, 2, 7, 11, 5);
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

		// Run-length style fillers: bit 0 = two zero rows, bits 10 = one zero row, bits 11 + payload = a non-zero row.
		private static void FillZeroRun(DecoderState state, Int32 col, Func<AcmBitReader, Int32> readNonZero)
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
					SetPos(state, i, col, readNonZero(state.Reader));
				}
			}
		}

		// One flag bit per row: 0 = zero, 1 + payload = a non-zero row.
		private static void FillEscaped(DecoderState state, Int32 col, Func<AcmBitReader, Int32> readNonZero)
		{
			for (Int32 i = 0; i < state.Rows; i++)
			{
				SetPos(state, i, col, state.Reader.ReadBits(1) == 0 ? 0 : readNonZero(state.Reader));
			}
		}

		// Several small values packed in one base-N symbol, first row in the lowest digit.
		private static void FillPacked(DecoderState state, Int32 col, Int32 valuesPerSymbol, Int32 symbolBits, Int32 radix, Int32 offset)
		{
			Int32 symbolLimit = 1;
			for (Int32 i = 0; i < valuesPerSymbol; i++)
			{
				symbolLimit *= radix;
			}

			for (Int32 row = 0; row < state.Rows; row += valuesPerSymbol)
			{
				Int32 symbol = state.Reader.ReadBits(symbolBits);
				if (symbol >= symbolLimit)
				{
					throw new InvalidDataException("The ACM stream contains an invalid packed symbol.");
				}

				for (Int32 k = 0; k < valuesPerSymbol && row + k < state.Rows; k++)
				{
					SetPos(state, row + k, col, symbol % radix - offset);
					symbol /= radix;
				}
			}
		}

		private static Int32 ReadMap1(AcmBitReader reader)
		{
			return Map1Bit[reader.ReadBits(1)];
		}

		private static Int32 ReadMap2Near(AcmBitReader reader)
		{
			return Map2BitNear[reader.ReadBits(2)];
		}

		private static Int32 ReadMap3(AcmBitReader reader)
		{
			return Map3Bit[reader.ReadBits(3)];
		}

		private static Int32 ReadMap1Or2Far(AcmBitReader reader)
		{
			return reader.ReadBits(1) == 0 ? Map1Bit[reader.ReadBits(1)] : Map2BitFar[reader.ReadBits(2)];
		}

		private static void SetPos(DecoderState state, Int32 row, Int32 col, Int32 index)
		{
			state.Block[(row << state.Level) + col] = state.MidBuffer[MidBufferCenter + index];
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