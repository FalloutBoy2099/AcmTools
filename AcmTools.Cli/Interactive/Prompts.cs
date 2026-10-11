namespace AcmTools.Cli.Interactive
{
	using System;
	using System.Globalization;
	using System.IO;
	using Spectre.Console;
	using Localization;

	internal static class Prompts
	{
		public static String ExistingPath(String label)
		{
			return AnsiConsole.Prompt(
				new TextPrompt<String>($"{label}:")
					.Validate(value => File.Exists(value)
						? ValidationResult.Success()
						: ValidationResult.Error(AppText.Get("FileNotFound"))));
		}

		public static String OutputPath(String label, String defaultValue)
		{
			return AnsiConsole.Prompt(
				new TextPrompt<String>($"{label}:")
					.DefaultValue(defaultValue)
					.Validate(value => String.IsNullOrWhiteSpace(value)
						? ValidationResult.Error(AppText.Get("RequiredPath"))
						: ValidationResult.Success()));
		}

		public static Int32 Integer(String label, Int32 defaultValue, Int32 min, Int32 max)
		{
			return AnsiConsole.Prompt(
				new TextPrompt<Int32>($"{label}:")
					.DefaultValue(defaultValue)
					.Validate(value => value >= min && value <= max
						? ValidationResult.Success()
						: ValidationResult.Error(AppText.Format("RangeError", min.ToString(CultureInfo.InvariantCulture), max.ToString(CultureInfo.InvariantCulture)))));
		}

		public static Boolean YesNo(String label, Boolean defaultValue)
		{
			return AnsiConsole.Prompt(
				new SelectionPrompt<Boolean>()
					.Title(label)
					.UseConverter(value => value ? AppText.Get("Yes") : AppText.Get("No"))
					.AddChoices(defaultValue, !defaultValue));
		}
	}
}