namespace AcmTools.Cli.Localization.Languages
{
	using System;
	using System.Collections.Generic;

	internal static class RussianTexts
	{
		public static IReadOnlyDictionary<String, String> Texts { get; } = new Dictionary<String, String>(StringComparer.Ordinal)
		{
			["MainMenuTitle"] = "Что вы хотите сделать?",
			["InspectAction"] = "Проверить ACM",
			["ToAudioAction"] = "Конвертировать ACM в аудио",
			["ToAcmAction"] = "Конвертировать аудио в ACM",
			["LanguageAction"] = "Изменить язык",
			["QuitAction"] = "Выход",
			["LanguageTitle"] = "Язык",
			["SourceAcm"] = "Исходный файл ACM",
			["TargetAudio"] = "Целевой аудиофайл",
			["SourceAudio"] = "Исходный аудиофайл",
			["TargetAcm"] = "Целевой файл ACM",
			["FormatTitle"] = "Формат вывода",
			["AutoFormat"] = "Автоматически ({0})",
			["FileNotFound"] = "Файл не найден.",
			["RequiredPath"] = "Требуется путь.",
			["Yes"] = "Да",
			["No"] = "Нет"
		};
	}
}