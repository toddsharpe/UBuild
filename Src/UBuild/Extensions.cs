using System;
using System.Collections.Generic;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;

namespace UBuild
{
	public static class Extensions
	{
		public static void CreateEntryFromAny(this ZipArchive archive, string sourceName, string entryName = "")
		{
			string fileName = Path.GetFileName(sourceName);
			if (File.GetAttributes(sourceName).HasFlag(FileAttributes.Directory))
			{
				archive.CreateEntryFromDirectory(sourceName, Path.Combine(entryName, fileName));
			}
			else
			{
				archive.CreateEntryFromFile(sourceName, Path.Combine(entryName, fileName));
			}
		}

		public static void CreateEntryFromDirectory(this ZipArchive archive, string sourceDirName, string entryName = "")
		{
			string[] files = Directory.GetFiles(sourceDirName).Concat(Directory.GetDirectories(sourceDirName)).ToArray();
			foreach (var file in files)
			{
				archive.CreateEntryFromAny(file, entryName);
			}
		}
	}
}