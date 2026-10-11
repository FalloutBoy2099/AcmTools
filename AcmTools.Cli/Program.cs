namespace AcmTools.Cli
{
	using System;

	internal static class Program
	{
		public static Task<Int32> Main(String[] args)
		{
			ConsoleEncoding.UseUtf8();
			return new CliApplication().RunAsync(args);
		}
	}
}