namespace AcmTools.Cli.Commands
{
	using System;
	using System.Collections.Generic;
	using System.IO;
	using Localization;

	internal sealed record BatchItem(String Input, String Output);

	/// <summary>Works out which files a folder or wildcard input stands for and where each converted file goes.</summary>
	internal static class BatchPlanner
	{
		/// <summary>True when the input stands for several files: an existing folder or a file name with wildcards.</summary>
		public static Boolean IsBatchInput(String input)
		{
			return Directory.Exists(input) || Path.GetFileName(input).IndexOfAny(['*', '?']) >= 0;
		}

		public static IReadOnlyList<BatchItem> Plan(String input, String outputDirectory, IReadOnlyCollection<String> extensions, String targetExtension, Boolean recursive)
		{
			String folder;
			String pattern;
			if (Directory.Exists(input))
			{
				folder = input;
				pattern = "*";
			}
			else
			{
				folder = Path.GetDirectoryName(input) is { Length: > 0 } directory ? directory : ".";
				pattern = Path.GetFileName(input);
				if (folder.IndexOfAny(['*', '?']) >= 0)
				{
					throw new CliUsageException(AppText.Format("BatchWildcardInFolder", input));
				}
			}

			if (File.Exists(outputDirectory))
			{
				throw new CliUsageException(AppText.Format("BatchOutputIsFile", outputDirectory));
			}

			SearchOption searchOption = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
			List<String> files = [];
			if (Directory.Exists(folder))
			{
				foreach (String file in Directory.EnumerateFiles(folder, pattern, searchOption))
				{
					if (extensions.Contains(Path.GetExtension(file), StringComparer.OrdinalIgnoreCase))
					{
						files.Add(file);
					}
				}
			}

			if (files.Count == 0)
			{
				throw new CliUsageException(AppText.Format("BatchNoFiles", input));
			}

			files.Sort(StringComparer.OrdinalIgnoreCase);
			List<BatchItem> items = [];
			HashSet<String> outputs = new(StringComparer.OrdinalIgnoreCase);
			foreach (String file in files)
			{
				String output = Path.ChangeExtension(Path.Combine(outputDirectory, Path.GetRelativePath(folder, file)), targetExtension);
				if (!outputs.Add(Path.GetFullPath(output)))
				{
					throw new CliUsageException(AppText.Format("BatchDuplicateOutput", output));
				}

				items.Add(new BatchItem(file, output));
			}

			return items;
		}
	}
}