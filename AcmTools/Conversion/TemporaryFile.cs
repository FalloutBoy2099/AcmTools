namespace AcmTools.Conversion
{
	using System;
	using System.IO;

	internal sealed class TemporaryFile : IDisposable
	{
		public TemporaryFile(String prefix = "acmtools", String extension = ".wav")
		{
			Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"{prefix}-{Guid.NewGuid():N}{extension}");
		}

		public String Path { get; }

		public void Dispose()
		{
			if (File.Exists(Path))
			{
				File.Delete(Path);
			}
		}
	}
}
