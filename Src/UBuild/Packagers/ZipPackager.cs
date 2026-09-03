using System;
using System.Collections.Generic;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;

namespace UBuild.Packagers
{
	public class ZipPackager : IPackager
	{
		public string DestFile { get; set; }

		private ZipArchive _archive;

		public ZipPackager(string destFile)
		{
			DestFile = destFile + ".zip";
		}

		public void Init()
		{
			_archive = ZipFile.Open(DestFile, ZipArchiveMode.Create);
		}

		public void AddEntries(string root, IEnumerable<string> entries)
		{
			foreach (string entry in entries)
			{
				_archive.CreateEntryFromAny(entry, root);
			}
		}

		public void Dispose()
		{
			_archive.Dispose();
		}
	}
}