namespace AcmTools.Cli.Localization
{
	using System;
	using System.Collections.Generic;
	using System.Globalization;
	using Languages;

	internal static partial class AppText
	{
		// Languages that do not define a key fall back to the English text.
		private static readonly Dictionary<CliLanguage, IReadOnlyDictionary<String, String>> Texts = new()
		{
			[CliLanguage.English] = EnglishTexts.Texts,
			[CliLanguage.French] = FrenchTexts.Texts,
			[CliLanguage.Spanish] = SpanishTexts.Texts,
			[CliLanguage.Italian] = ItalianTexts.Texts,
			[CliLanguage.German] = GermanTexts.Texts,
			[CliLanguage.Portuguese] = PortugueseTexts.Texts,
			[CliLanguage.Polish] = PolishTexts.Texts,
			[CliLanguage.Czech] = CzechTexts.Texts,
			[CliLanguage.Swedish] = SwedishTexts.Texts,
			[CliLanguage.Chinese] = ChineseTexts.Texts,
			[CliLanguage.Russian] = RussianTexts.Texts
		};

		public static IReadOnlyList<LanguageOption> Languages { get; } =
		[
			new(CliLanguage.English, "en", "en-US", "English"),
			new(CliLanguage.French, "fr", "fr-FR", "Français"),
			new(CliLanguage.Spanish, "es", "es-ES", "Español"),
			new(CliLanguage.Italian, "it", "it-IT", "Italiano"),
			new(CliLanguage.German, "de", "de-DE", "Deutsch"),
			new(CliLanguage.Portuguese, "pt", "pt-PT", "Português"),
			new(CliLanguage.Polish, "pl", "pl-PL", "Polski"),
			new(CliLanguage.Czech, "cs", "cs-CZ", "Čeština"),
			new(CliLanguage.Swedish, "sv", "sv-SE", "Svenska"),
			new(CliLanguage.Chinese, "zh", "zh-CN", "中文"),
			new(CliLanguage.Russian, "ru", "ru-RU", "Русский")
		];

		public static CliLanguage Current { get; private set; } = DetectSystemLanguage();

		/// <summary>The codes accepted by <c>--lang</c>, for messages: "en, fr, es...".</summary>
		public static String LanguageCodes => String.Join(", ", Languages.Select(option => option.Code));

		public static void SetLanguage(CliLanguage language)
		{
			Current = language;
			CultureInfo culture = CultureInfo.GetCultureInfo(Languages.First(option => option.Language == language).CultureName);
			CultureInfo.CurrentCulture = culture;
			CultureInfo.CurrentUICulture = culture;
		}

		/// <summary>Finds a language from its code ("fr"), culture ("fr-FR", "fr_CA"), enum name ("French") or display name ("Français").</summary>
		public static Boolean TryParseLanguage(String? text, out CliLanguage language)
		{
			language = CliLanguage.English;
			String? trimmed = text?.Trim();
			if (String.IsNullOrEmpty(trimmed))
			{
				return false;
			}

			String code = trimmed.Split('-', '_')[0];
			foreach (LanguageOption option in Languages)
			{
				if (String.Equals(option.Code, code, StringComparison.OrdinalIgnoreCase)
					|| String.Equals(option.Language.ToString(), trimmed, StringComparison.OrdinalIgnoreCase)
					|| String.Equals(option.DisplayName, trimmed, StringComparison.CurrentCultureIgnoreCase))
				{
					language = option.Language;
					return true;
				}
			}

			return false;
		}

		public static String Get(String key)
		{
			if (Texts[Current].TryGetValue(key, out String? value))
			{
				return value;
			}

			return Texts[CliLanguage.English][key];
		}

		public static String Format(String key, params Object[] values)
		{
			return String.Format(CultureInfo.CurrentCulture, Get(key), values);
		}

		internal static IReadOnlyDictionary<String, String> GetTexts(CliLanguage language)
		{
			return Texts[language];
		}

		private static CliLanguage DetectSystemLanguage()
		{
			String code = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
			return Languages.FirstOrDefault(option => String.Equals(option.Code, code, StringComparison.OrdinalIgnoreCase)).Language;
		}
	}
}