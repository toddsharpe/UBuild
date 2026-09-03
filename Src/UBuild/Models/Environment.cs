using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace UBuild.Models
{
	public class Environment
	{
		internal const string FileName = "Env_build.json";

		public string Output { get; set; }
		public string Sources { get; set; }
		public string Exes { get; set; }
		public string Configs { get; set; }
		//Applied to every exe, so shared include roots and defines are declared once
		public List<string> IncludeDirs { get; set; } = new List<string>();
		public List<string> Defines { get; set; } = new List<string>();
		public List<Project> Projects { get; set;} = new List<Project>();
		public List<Script> Scripts { get; set;} = new List<Script>();
		public List<Toolchain> Toolchains { get; set; } = new List<Toolchain>();

		internal string OutputObj => Output + "_obj";
		internal string OutputExe => Output + "_exe";

		internal string SourcesDirectory => Path.Combine(Directory, Sources);
		internal string OutputObjectDirectory => Path.Combine(Directory, OutputObj);
		internal string OutputExeDirectory => Path.Combine(Directory, OutputExe);
		internal string ExeDirectory => Path.Combine(Directory, Exes);
		internal string ConfigsDirectory => Path.Combine(Directory, Configs);
		internal string Directory { get; set; }

		internal static Environment Load(string filename)
		{
			Environment env = Config.Config.ReadJson(filename, Config.UBuildJsonContext.Default.Environment);
			env.Directory = Path.GetDirectoryName(filename);

			return env;
		}

		internal Executable GetExe(string path)
		{
			string file = Path.Combine(ExeDirectory, (path ?? "") + "_exe.json");
			if (!File.Exists(file))
				return null;

			Executable exe = Config.Config.ReadJson(file, Config.UBuildJsonContext.Default.Executable);

			//One level of inheritance, so a board's recipe is its own lines rather than the shared skeleton again
			if (!string.IsNullOrWhiteSpace(exe.Extends))
			{
				string baseFile = Path.Combine(ExeDirectory, exe.Extends + "_exe.json");
				if (!File.Exists(baseFile))
					throw new Exception($"{file} extends '{exe.Extends}', but there is no {baseFile}");

				Executable basis = Config.Config.ReadJson(baseFile, Config.UBuildJsonContext.Default.Executable);
				if (!string.IsNullOrWhiteSpace(basis.Extends))
					throw new Exception($"{baseFile} extends '{basis.Extends}', and Extends is one level only");

				exe = Executable.Merge(basis, exe);
			}

			exe.OutDir = Path.Combine(OutputExeDirectory, path);
			exe.RelPath = path;

			return exe;
		}

		//Objects live under <toolchain>/<exe> so two toolchains never write the same file
		internal string GetObjectPath(string source, string toolchain, string exe)
		{
			//Only the final extension changes; a directory like "v1.cache" must survive intact
			string objectFile = Path.ChangeExtension(source, ".o");

			//A generated source reaches out of Sources with "..", which would land the object beside the exe directory instead of inside it
			string contained = string.Join('/', objectFile.Split('/').Select(i => i == ".." ? "__" : i));
			return Path.Combine(OutputObjectDirectory, toolchain, exe, contained);
		}

		internal Executable RequireExe(string path)
		{
			return GetExe(path) ?? throw new Exception($"Exe '{path}' not found: no {Path.Combine(ExeDirectory, (path ?? "") + "_exe.json")}");
		}

		internal Toolchain FindToolchain(string name)
		{
			return Toolchains.FirstOrDefault(i => i.Name == name)
				?? throw new Exception($"Toolchain '{name}' not found. Defined: {Named(Toolchains.Select(i => i.Name))}");
		}

		internal Script FindScript(string name)
		{
			return Scripts.FirstOrDefault(i => i.Name == name)
				?? throw new Exception($"Script '{name}' not found. Defined: {Named(Scripts.Select(i => i.Name))}");
		}

		internal Project FindProject(string name)
		{
			return Projects.FirstOrDefault(i => i.Name == name)
				?? throw new Exception($"Project '{name}' not found. Defined: {Named(Projects.Select(i => i.Name))}");
		}

		//The project entries already say how an exe is built, so -t is only needed where they disagree
		internal List<string> WaysToBuild(string exe)
		{
			return Projects
				.SelectMany(i => i.Exes ?? new List<Project.ExeEntry>())
				.Where(i => i.Name == exe)
				.Select(i => !string.IsNullOrWhiteSpace(i.Toolchain) ? i.Toolchain : i.Script)
				.Where(i => !string.IsNullOrWhiteSpace(i))
				.Distinct()
				.ToList();
		}

		private static string Named(IEnumerable<string> names)
		{
			string joined = string.Join(", ", names);
			return string.IsNullOrEmpty(joined) ? "none" : joined;
		}

		internal string GetSourcePath(string source)
		{
			return Path.Combine(SourcesDirectory, source);
		}
	}
}
