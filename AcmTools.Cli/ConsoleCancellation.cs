namespace AcmTools.Cli
{
	using System;
	using System.Threading;

	/// <summary>Turns Ctrl+C into a cancellation token while it is alive, instead of killing the process in the middle of a write.</summary>
	internal sealed class ConsoleCancellation : IDisposable
	{
		private readonly CancellationTokenSource source = new();

		public ConsoleCancellation()
		{
			Console.CancelKeyPress += OnCancelKeyPress;
		}

		public CancellationToken Token => source.Token;

		public void Dispose()
		{
			Console.CancelKeyPress -= OnCancelKeyPress;
			source.Dispose();
		}

		private void OnCancelKeyPress(Object? sender, ConsoleCancelEventArgs e)
		{
			e.Cancel = true;
			try
			{
				source.Cancel();
			}
			catch (ObjectDisposedException)
			{
			}
		}
	}
}