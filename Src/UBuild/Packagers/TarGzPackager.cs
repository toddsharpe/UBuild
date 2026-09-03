using System.Formats.Tar;
using System.IO.Compression;

namespace UBuild.Packagers
{
	public class TarGzPackager : IPackager
	{
		public string DestFile { get; set; }

		private FileStream _file;
		private GZipStream _gzip;
		private TarWriter _tar;

		public TarGzPackager(string destFile)
		{
			DestFile = destFile + ".tar.gz";
		}

		public void Init()
		{
			_file = File.Create(DestFile);
			_gzip = new GZipStream(_file, CompressionMode.Compress);
			_tar = new TarWriter(_gzip, TarEntryFormat.Pax);
		}

		public void AddEntries(string root, IEnumerable<string> entries)
		{
			foreach (string entry in entries)
				AddEntry(root, entry);
		}

		private void AddEntry(string root, string source)
		{
			string name = Path.GetFileName(source);

			if (File.GetAttributes(source).HasFlag(FileAttributes.Directory))
			{
				foreach (string child in Directory.GetFileSystemEntries(source))
					AddEntry(Path.Combine(root, name), child);
			}
			else
			{
				//Tar always uses forward slashes, whatever built the path
				_tar.WriteEntry(source, Path.Combine(root, name).Replace('\\', '/'));
			}
		}

		public void Dispose()
		{
			_tar?.Dispose();
			_gzip?.Dispose();
			_file?.Dispose();
		}
	}
}
