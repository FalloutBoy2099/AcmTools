namespace AcmTools.Tests
{
	using System;
	using System.Collections.Generic;
	using System.IO;
	using System.Linq;
	using System.Text.RegularExpressions;
	using AcmTools.Acm;
	using AcmTools.Audio;
	using AcmTools.Cli.Localization;
	using AcmTools.Conversion;

	public sealed class LocalizationTests
	{
		public static IEnumerable<Object[]> Languages()
		{
			return Enum.GetNames<CliLanguage>().Select(name => new Object[] { name });
		}

		[Theory]
		[MemberData(nameof(Languages))]
		public void EveryLanguageOnlyDefinesKnownKeys(String languageName)
		{
			CliLanguage language = Enum.Parse<CliLanguage>(languageName);
			IReadOnlyDictionary<String, String> english = AppText.GetTexts(CliLanguage.English);

			foreach (String key in AppText.GetTexts(language).Keys)
			{
				Assert.True(english.ContainsKey(key), $"{language} defines '{key}' which does not exist in English.");
			}
		}

		[Theory]
		[MemberData(nameof(Languages))]
		public void EveryLanguageKeepsTheEnglishPlaceholders(String languageName)
		{
			CliLanguage language = Enum.Parse<CliLanguage>(languageName);
			IReadOnlyDictionary<String, String> english = AppText.GetTexts(CliLanguage.English);

			foreach ((String key, String text) in AppText.GetTexts(language))
			{
				Assert.Equal(Placeholders(english[key]), Placeholders(text));
			}
		}

		[Fact]
		public void FrenchTranslatesEveryEnglishKey()
		{
			IReadOnlyDictionary<String, String> english = AppText.GetTexts(CliLanguage.English);
			IReadOnlyDictionary<String, String> french = AppText.GetTexts(CliLanguage.French);

			Assert.Equal(english.Keys.Order(), french.Keys.Order());
		}

		[Theory]
		[MemberData(nameof(Languages))]
		public void CompactLanguagePacksTranslateTheBasicKeys(String languageName)
		{
			CliLanguage language = Enum.Parse<CliLanguage>(languageName);
			IReadOnlyDictionary<String, String> english = AppText.GetTexts(CliLanguage.English);
			IReadOnlyDictionary<String, String> texts = AppText.GetTexts(language);

			foreach (String key in AppText.BasicKeys)
			{
				Assert.True(texts.ContainsKey(key), $"{language} is missing '{key}'.");
				if (language != CliLanguage.English && key != "No")
				{
					Assert.NotEqual(english[key], texts[key]);
				}
			}
		}

		[Fact]
		public void FrenchTranslatesEveryConversionStepProducedByTheLibrary()
		{
			CliLanguage previous = AppText.Current;
			String directory = Path.Combine(Path.GetTempPath(), $"acmtools-test-{Guid.NewGuid():N}");
			Directory.CreateDirectory(directory);
			try
			{
				Byte[] bytes = new Byte[2000];
				PcmAudioTrack source = new(22050, 1, 16, bytes);
				String acm = Path.Combine(directory, "silence.acm");
				String wav = Path.Combine(directory, "silence.wav");
				AcmConverter converter = new();

				List<String> steps = [];
				steps.AddRange(converter.ConvertAudioToAcmAsync(WriteWav(directory, source, 22050), acm).Result.Steps);
				steps.AddRange(converter.ConvertAudioToAcmAsync(WriteWav(directory, source, 44100), acm).Result.Steps);
				steps.AddRange(converter.ConvertAcmToAudioAsync(acm, wav).Result.Steps);

				AppText.SetLanguage(CliLanguage.French);
				foreach (String step in steps)
				{
					Assert.NotEqual(step, AppText.LocalizeStep(step));
				}

				AppText.SetLanguage(CliLanguage.English);
				foreach (String step in steps)
				{
					Assert.Equal(step, AppText.LocalizeStep(step));
				}
			}
			finally
			{
				AppText.SetLanguage(previous);
				Directory.Delete(directory, true);
			}
		}

		private static String WriteWav(String directory, PcmAudioTrack source, Int32 sampleRate)
		{
			String path = Path.Combine(directory, $"in-{sampleRate}.wav");
			new WavPcmWriter().Write(path, new PcmAudioTrack(sampleRate, source.Channels, source.BitsPerSample, source.Samples));
			return path;
		}

		private static String[] Placeholders(String text)
		{
			return Regex.Matches(text, @"\{\d+\}").Select(match => match.Value).Order().ToArray();
		}
	}
}
