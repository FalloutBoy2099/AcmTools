namespace AcmTools.Cli
{
	using System;
	using AcmTools.Cli.Localization;

	internal static class Program
	{
		public static Task<Int32> Main(String[] args)
		{
			ConsoleEncoding.UseUtf8();
			AppText.SetLanguage(AppText.Current);
			return new CliApplication().RunAsync(args);
		}
	}
}