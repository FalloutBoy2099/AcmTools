namespace AcmTools.Cli.Localization.Languages
{
	using System;
	using System.Collections.Generic;

	internal static class ChineseTexts
	{
		public static IReadOnlyDictionary<String, String> Texts { get; } = new Dictionary<String, String>(StringComparer.Ordinal)
		{
			["MainMenuTitle"] = "你想做什么？",
			["InspectAction"] = "检查 ACM",
			["ToAudioAction"] = "将 ACM 转换为音频",
			["ToAcmAction"] = "将音频转换为 ACM",
			["LanguageAction"] = "更改语言",
			["QuitAction"] = "退出",
			["LanguageTitle"] = "语言",
			["SourceAcm"] = "源 ACM 文件",
			["TargetAudio"] = "目标音频文件",
			["SourceAudio"] = "源音频文件",
			["TargetAcm"] = "目标 ACM 文件",
			["FormatTitle"] = "输出格式",
			["AutoFormat"] = "自动（{0}）",
			["FileNotFound"] = "找不到文件。",
			["RequiredPath"] = "必须提供路径。",
			["Yes"] = "是",
			["No"] = "否"
		};
	}
}