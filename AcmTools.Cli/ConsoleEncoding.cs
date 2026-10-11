namespace AcmTools.Cli
{
	using System;
	using System.IO;
	using System.Text;

	internal static class ConsoleEncoding
	{
		/// <summary>Switches the console to UTF-8 (the setters also update the Windows console code pages). Silently ignored when there is no console.</summary>
		public static void UseUtf8()
		{
			try
			{
				Console.InputEncoding = Encoding.UTF8;
				Console.OutputEncoding = Encoding.UTF8;
			}
			catch (IOException)
			{
			}
			catch (UnauthorizedAccessException)
			{
			}
		}
	}
}