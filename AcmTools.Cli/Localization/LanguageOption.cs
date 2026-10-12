namespace AcmTools.Cli.Localization
{
	using System;

	/// <param name="Code">ISO 639-1 code, accepted by <c>--lang</c>.</param>
	/// <param name="CultureName">Culture applied to number and date formatting.</param>
	/// <param name="DisplayName">Name of the language written in that language.</param>
	internal readonly record struct LanguageOption(CliLanguage Language, String Code, String CultureName, String DisplayName);
}