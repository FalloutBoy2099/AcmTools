namespace AcmTools.Acm
{
	using System;

	/// <summary>
	/// The sub-band transform of the ACM codec, in double precision.
	/// <para>
	/// The decoder rebuilds the audio from a block of coefficients with <c>level</c> synthesis stages. A stage works on
	/// the interleaved sequences <c>x[i], x[i + s], x[i + 2s], ...</c> (stride <c>s</c>, from <c>columns / 2</c> down to 1)
	/// and treats them as pairs (low, high) = (s[j], d[j]) that become the samples
	/// <c>y[2j] = s[j] + s[j-1] + 2 d[j-1]</c> and <c>y[2j+1] = 2 s[j] - d[j] - d[j-1]</c>.
	/// </para>
	/// <para>
	/// This is a 2x2 FIR system with a determinant that is not a pure delay, so its inverse is an IIR filter with one
	/// stable and one unstable pole (3 +- 2 sqrt 2). <see cref="Analyze"/> inverts it by splitting the recursion into its
	/// two modes: one is run forward in time and the other backward, which keeps every coefficient bounded.
	/// </para>
	/// </summary>
	internal static class AcmTransform
	{
		private static readonly Double Lambda1 = 3 + 2 * Math.Sqrt(2);
		private static readonly Double Lambda2 = 3 - 2 * Math.Sqrt(2);
		private static readonly Double EigenNorm = Math.Sqrt(1 + Math.Pow(1 + Math.Sqrt(2), 2));
		private static readonly Double U0 = 1 / EigenNorm;
		private static readonly Double U1 = (1 + Math.Sqrt(2)) / EigenNorm;
		private static readonly Double V0 = -(1 + Math.Sqrt(2)) / EigenNorm;
		private static readonly Double V1 = 1 / EigenNorm;

		/// <summary>Coefficients to samples, in place, exactly like the decoder (the length must be a multiple of 2^level).</summary>
		/// <param name="roundingOffsets">Adds the +1 the decoder applies after its first stage.</param>
		public static void Synthesize(Double[] data, Int32 level, Boolean roundingOffsets)
		{
			Int32 columns = 1 << level;
			for (Int32 stage = 0; stage < level; stage++)
			{
				Int32 stride = columns >> (stage + 1);
				for (Int32 i = 0; i < stride; i++)
				{
					Double previousLow = 0;
					Double previousHigh = 0;
					for (Int32 position = i; position + stride < data.Length; position += 2 * stride)
					{
						Double low = data[position];
						Double high = data[position + stride];
						data[position] = low + previousLow + 2 * previousHigh;
						data[position + stride] = 2 * low - high - previousHigh;
						previousLow = low;
						previousHigh = high;
					}
				}

				if (stage == 0 && roundingOffsets)
				{
					for (Int32 n = 0; n < data.Length; n += columns >> 1)
					{
						data[n] += 1;
					}
				}
			}
		}

		/// <summary>
		/// Samples to coefficients, in place: the inverse of <see cref="Synthesize"/> for a signal that is zero
		/// before the start and after the end of the array (the decoder starts from a zero state).
		/// </summary>
		public static void Analyze(Double[] data, Int32 level, Boolean roundingOffsets)
		{
			Int32 columns = 1 << level;
			for (Int32 stage = level - 1; stage >= 0; stage--)
			{
				if (stage == 0 && roundingOffsets)
				{
					for (Int32 n = 0; n < data.Length; n += columns >> 1)
					{
						data[n] -= 1;
					}
				}

				AnalyzeStage(data, columns >> (stage + 1));
			}
		}

		/// <summary>
		/// Variance added to each output sample (in the units of the decoder output, i.e. after the final shift)
		/// per unit of quantization noise variance on every coefficient.
		/// </summary>
		public static Double NoiseGain(Int32 level)
		{
			Int32 columns = 1 << level;
			Int32 rows = 8;
			Double total = 0;
			for (Int32 column = 0; column < columns; column++)
			{
				Double[] impulse = new Double[rows * columns];
				impulse[3 * columns + column] = 1;
				Synthesize(impulse, level, false);
				foreach (Double sample in impulse)
				{
					total += sample * sample;
				}
			}

			return total / columns / Math.Pow(4, level);
		}

		private static void AnalyzeStage(Double[] data, Int32 stride)
		{
			Int32 pairs = data.Length / (2 * stride);
			Double[] lowMode = new Double[pairs];
			Double[] highMode = new Double[pairs];
			for (Int32 i = 0; i < stride; i++)
			{
				// W = A0 * Y with A0 = [[1, 0], [2, -1]]; X[j] = W[j] - B * X[j-1] with B = [[1, 2], [2, 5]].
				Double previousB = 0;
				for (Int32 j = 0; j < pairs; j++)
				{
					Int32 position = i + 2 * j * stride;
					Double even = data[position];
					Double odd = data[position + stride];
					Double w0 = even;
					Double w1 = 2 * even - odd;
					Double wa = U0 * w0 + U1 * w1;
					Double wb = V0 * w0 + V1 * w1;
					previousB = wb - Lambda2 * previousB;
					highMode[j] = previousB;
					lowMode[j] = wa;
				}

				// The unstable mode is solved backward, assuming the signal stays zero after the end.
				Double next = 0;
				for (Int32 j = pairs - 1; j >= 0; j--)
				{
					Double wa = lowMode[j];
					lowMode[j] = next;
					next = (wa - next) / Lambda1;
				}

				for (Int32 j = 0; j < pairs; j++)
				{
					Int32 position = i + 2 * j * stride;
					Double a = lowMode[j];
					Double b = highMode[j];
					data[position] = a * U0 + b * V0;
					data[position + stride] = a * U1 + b * V1;
				}
			}
		}
	}
}