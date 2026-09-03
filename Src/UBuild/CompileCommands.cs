using System.Text.Encodings.Web;
using System.Text.Json;
using UBuild.Models;

namespace UBuild
{
	//Per-source compile commands, written as compile_commands.json for clangd and friends
	internal static class CompileCommands
	{
		internal const string FileName = "compile_commands.json";

		private static readonly List<CompileCommand> _entries = new List<CompileCommand>();

		//The default encoder escapes '+', and this file is meant to be read by people too
		private static readonly Config.UBuildJsonContext _context = new Config.UBuildJsonContext(new JsonSerializerOptions
		{
			WriteIndented = true,
			Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
		});

		internal static void Add(string directory, string file, string command)
		{
			//Compiles are queued from one thread today, but the lock keeps that from being load bearing
			lock (_entries)
			{
				_entries.Add(new CompileCommand
				{
					Directory = directory,
					File = file,
					Command = command,
				});
			}
		}

		internal static void Write(string directory)
		{
			lock (_entries)
			{
				if (_entries.Count == 0)
					return;

				string path = Path.Combine(directory, FileName);

				//Merged onto what is already there, so building one exe leaves the rest of the tree indexed
				Dictionary<string, CompileCommand> merged = new Dictionary<string, CompileCommand>();
				foreach (CompileCommand entry in Existing(path).Concat(_entries))
					merged[entry.Directory + "\0" + entry.File] = entry;

				List<CompileCommand> all = merged.Values.OrderBy(i => i.File, StringComparer.Ordinal).ToList();
				File.WriteAllText(path, JsonSerializer.Serialize(all, _context.ListCompileCommand));
			}
		}

		//A database we cannot read is one this build is replacing anyway, so it is not worth failing over
		private static List<CompileCommand> Existing(string path)
		{
			if (!File.Exists(path))
				return new List<CompileCommand>();

			try
			{
				return JsonSerializer.Deserialize(File.ReadAllText(path), _context.ListCompileCommand) ?? new List<CompileCommand>();
			}
			catch (JsonException)
			{
				return new List<CompileCommand>();
			}
		}
	}
}
