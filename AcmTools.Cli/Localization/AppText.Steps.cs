namespace AcmTools.Cli.Localization
{
	using System;
	using System.Linq;
	using AcmTools.Conversion;

	internal static partial class AppText
	{
		/// <summary>Text of a conversion step in the current language (keys are "Step" + <see cref="ConversionStepKind"/>).</summary>
		public static String LocalizeStep(ConversionStep step)
		{
			return Format("Step" + step.Kind, step.Arguments.Cast<Object>().ToArray());
		}
	}
}