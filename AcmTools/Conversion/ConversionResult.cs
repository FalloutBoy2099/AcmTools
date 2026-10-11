namespace AcmTools.Conversion
{
	using System;
	using System.Collections.Generic;

	public sealed record ConversionResult(
		String InputPath,
		String OutputPath,
		IReadOnlyList<String> Steps);
}