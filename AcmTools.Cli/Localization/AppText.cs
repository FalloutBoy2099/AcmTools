namespace AcmTools.Cli.Localization
{
	using System;
	using System.Collections.Generic;
	using System.Globalization;
	using System.Linq;

	internal static partial class AppText
	{
		// Keys translated by the compact language packs (everything else falls back to English), in the order expected by Basic().
		internal static readonly IReadOnlyList<String> BasicKeys =
		[
			"MainMenuTitle", "InspectAction", "ToAudioAction", "ToAcmAction", "LanguageAction", "QuitAction", "LanguageTitle",
			"SourceAcm", "TargetAudio", "SourceAudio", "TargetAcm", "FormatTitle", "AutoFormat", "FileNotFound", "RequiredPath",
			"Yes", "No"
		];

		private static readonly Dictionary<CliLanguage, Dictionary<String, String>> Texts = CreateTexts();

		public static CliLanguage Current { get; private set; } = DetectSystemLanguage();

		public static IReadOnlyList<LanguageOption> Languages { get; } =
		[
			new(CliLanguage.English, "English"),
			new(CliLanguage.French, "Français"),
			new(CliLanguage.Spanish, "Español"),
			new(CliLanguage.Italian, "Italiano"),
			new(CliLanguage.German, "Deutsch"),
			new(CliLanguage.Portuguese, "Português"),
			new(CliLanguage.Polish, "Polski"),
			new(CliLanguage.Czech, "Čeština"),
			new(CliLanguage.Swedish, "Svenska"),
			new(CliLanguage.Chinese, "中文"),
			new(CliLanguage.Russian, "Русский")
		];

		public static void SetLanguage(CliLanguage language)
		{
			Current = language;
			String cultureName = language switch
			{
				CliLanguage.French => "fr-FR",
				CliLanguage.Spanish => "es-ES",
				CliLanguage.Italian => "it-IT",
				CliLanguage.German => "de-DE",
				CliLanguage.Portuguese => "pt-PT",
				CliLanguage.Polish => "pl-PL",
				CliLanguage.Czech => "cs-CZ",
				CliLanguage.Swedish => "sv-SE",
				CliLanguage.Chinese => "zh-CN",
				CliLanguage.Russian => "ru-RU",
				_ => "en-US"
			};
			CultureInfo culture = CultureInfo.GetCultureInfo(cultureName);
			CultureInfo.CurrentCulture = culture;
			CultureInfo.CurrentUICulture = culture;
		}

		public static String Get(String key)
		{
			if (Texts.TryGetValue(Current, out Dictionary<String, String>? current) && current.TryGetValue(key, out String? value))
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
			return CultureInfo.CurrentUICulture.TwoLetterISOLanguageName.ToLowerInvariant() switch
			{
				"fr" => CliLanguage.French,
				"es" => CliLanguage.Spanish,
				"it" => CliLanguage.Italian,
				"de" => CliLanguage.German,
				"pt" => CliLanguage.Portuguese,
				"pl" => CliLanguage.Polish,
				"cs" => CliLanguage.Czech,
				"sv" => CliLanguage.Swedish,
				"zh" => CliLanguage.Chinese,
				"ru" => CliLanguage.Russian,
				_ => CliLanguage.English
			};
		}

		private static Dictionary<CliLanguage, Dictionary<String, String>> CreateTexts()
		{
			Dictionary<String, String> english = CreateEnglishTexts();

			return new Dictionary<CliLanguage, Dictionary<String, String>>
			{
				[CliLanguage.English] = english,
				[CliLanguage.French] = Merge(english, CreateFrenchTexts()),
				[CliLanguage.Spanish] = Merge(english, Basic("¿Qué quieres hacer?", "Inspeccionar un ACM", "Convertir ACM a audio", "Convertir audio a ACM", "Cambiar idioma", "Salir", "Idioma", "Archivo ACM de origen", "Archivo de audio de destino", "Archivo de audio de origen", "Archivo ACM de destino", "Formato de salida", "Automático ({0})", "Archivo no encontrado.", "Ruta obligatoria.", "Sí", "No")),
				[CliLanguage.Italian] = Merge(english, Basic("Cosa vuoi fare?", "Ispeziona un ACM", "Converti ACM in audio", "Converti audio in ACM", "Cambia lingua", "Esci", "Lingua", "File ACM sorgente", "File audio di destinazione", "File audio sorgente", "File ACM di destinazione", "Formato di uscita", "Automatico ({0})", "File non trovato.", "Percorso obbligatorio.", "Sì", "No")),
				[CliLanguage.German] = Merge(english, Basic("Was möchtest du tun?", "ACM prüfen", "ACM in Audio konvertieren", "Audio in ACM konvertieren", "Sprache ändern", "Beenden", "Sprache", "Quell-ACM-Datei", "Ziel-Audiodatei", "Quell-Audiodatei", "Ziel-ACM-Datei", "Ausgabeformat", "Automatisch ({0})", "Datei nicht gefunden.", "Pfad ist erforderlich.", "Ja", "Nein")),
				[CliLanguage.Portuguese] = Merge(english, Basic("O que deseja fazer?", "Inspecionar um ACM", "Converter ACM para áudio", "Converter áudio para ACM", "Alterar idioma", "Sair", "Idioma", "Arquivo ACM de origem", "Arquivo de áudio de destino", "Arquivo de áudio de origem", "Arquivo ACM de destino", "Formato de saída", "Automático ({0})", "Arquivo não encontrado.", "Caminho obrigatório.", "Sim", "Não")),
				[CliLanguage.Polish] = Merge(english, Basic("Co chcesz zrobić?", "Sprawdź ACM", "Konwertuj ACM na audio", "Konwertuj audio na ACM", "Zmień język", "Zakończ", "Język", "Źródłowy plik ACM", "Docelowy plik audio", "Źródłowy plik audio", "Docelowy plik ACM", "Format wyjściowy", "Automatycznie ({0})", "Nie znaleziono pliku.", "Ścieżka jest wymagana.", "Tak", "Nie")),
				[CliLanguage.Czech] = Merge(english, Basic("Co chceš udělat?", "Zkontrolovat ACM", "Převést ACM na audio", "Převést audio na ACM", "Změnit jazyk", "Konec", "Jazyk", "Zdrojový soubor ACM", "Cílový zvukový soubor", "Zdrojový zvukový soubor", "Cílový soubor ACM", "Výstupní formát", "Automaticky ({0})", "Soubor nenalezen.", "Cesta je povinná.", "Ano", "Ne")),
				[CliLanguage.Swedish] = Merge(english, Basic("Vad vill du göra?", "Inspektera en ACM", "Konvertera ACM till ljud", "Konvertera ljud till ACM", "Ändra språk", "Avsluta", "Språk", "ACM-fil (källa)", "Ljudfil (mål)", "Ljudfil (källa)", "ACM-fil (mål)", "Utdataformat", "Automatiskt ({0})", "Filen hittades inte.", "Sökväg krävs.", "Ja", "Nej")),
				[CliLanguage.Chinese] = Merge(english, Basic("你想做什么？", "检查 ACM", "将 ACM 转换为音频", "将音频转换为 ACM", "更改语言", "退出", "语言", "源 ACM 文件", "目标音频文件", "源音频文件", "目标 ACM 文件", "输出格式", "自动（{0}）", "找不到文件。", "必须提供路径。", "是", "否")),
				[CliLanguage.Russian] = Merge(english, Basic("Что вы хотите сделать?", "Проверить ACM", "Конвертировать ACM в аудио", "Конвертировать аудио в ACM", "Изменить язык", "Выход", "Язык", "Исходный файл ACM", "Целевой аудиофайл", "Исходный аудиофайл", "Целевой файл ACM", "Формат вывода", "Автоматически ({0})", "Файл не найден.", "Требуется путь.", "Да", "Нет"))
			};
		}

		private static Dictionary<String, String> Basic(params String[] values)
		{
			if (values.Length != BasicKeys.Count)
			{
				throw new ArgumentException($"Expected {BasicKeys.Count} texts, got {values.Length}.", nameof(values));
			}

			return BasicKeys.Zip(values).ToDictionary(pair => pair.First, pair => pair.Second, StringComparer.Ordinal);
		}

		private static Dictionary<String, String> Merge(Dictionary<String, String> fallback, Dictionary<String, String> overlay)
		{
			Dictionary<String, String> merged = new(fallback, StringComparer.Ordinal);
			foreach (KeyValuePair<String, String> pair in overlay)
			{
				merged[pair.Key] = pair.Value;
			}

			return merged;
		}
	}
}
