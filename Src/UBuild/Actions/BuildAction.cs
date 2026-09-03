using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UBuild.Tasks;
using UBuild.Models;
using System.Diagnostics;
using System.Reflection;
using Environment = UBuild.Models.Environment;

namespace UBuild.Actions
{
	internal class BuildAction : IAction
	{
		enum SourceType
		{
			C,
			Cpp,
			Asm
		}

		//Sources per generated unity file: enough to amortize headers, small enough to keep batches in flight
		private const int DefaultBatchSize = 8;

		private static readonly Dictionary<SourceType, List<string>> SourceExtensions = new Dictionary<SourceType, List<string>>
		{
			{ SourceType.C, new List<string> { ".c" } },
			{ SourceType.Cpp, new List<string> { ".cc", ".cpp" } },
			{ SourceType.Asm, new List<string> { ".s" } },
		};

		private readonly Environment _env;
		private readonly Executable _exe;
		private readonly Toolchain _toolchain;
		private readonly BuildOptions _options;
		public string OutputFile => Path.Combine(_exe.OutDir, _exe.Name + _toolchain.Ext);
		public string Label => $"{_exe.Name} ({_toolchain.Name})";

		internal BuildAction(Environment env, Executable exe, Toolchain toolchain, BuildOptions options)
		{
			_env = env;
			_exe = exe;
			_toolchain = toolchain;
			_options = options;
		}

		public ActionResult Run(bool verbose, Output output)
		{
			output.Line($"Building {_exe.Name} for {_toolchain.Name}");

			//Pre/post build env
			Dictionary<string, string> env = new Dictionary<string, string>
			{
				{ "BinFile", _exe.BinFile },
				{ "OutDir", _exe.OutDir },
				{ "SrcDir", _env.SourcesDirectory },
				//$BinFile carries no extension, so a step that wants the artifact itself has this
				{ "OutFile", OutputFile },
				//The reflection below exports the toolchain's own properties, none of which reads as its name
				{ "Toolchain", _toolchain.Name },
			};
			foreach (PropertyInfo property in typeof(Toolchain).GetProperties(BindingFlags.GetProperty | BindingFlags.Public | BindingFlags.Instance))
			{
				if (property.PropertyType != typeof(string))
					continue;
				
				string value = property.GetValue(_toolchain) as string;
				env.Add(property.Name, value ?? string.Empty);
			}

			//Pre build, runs before sources are enumerated so generated files are picked up
			Directory.CreateDirectory(Path.GetDirectoryName(OutputFile));
			if (!Runner.Run(_exe.PreBuild.Select(i => StepTask(i, env)), verbose, output))
				return ActionResult.Failed;

			List<ITask> compiles = new List<ITask>();
			List<string> objects = new List<string>();

			//Group files by type
			Dictionary<string, List<string>> groups = ExpandedSources(_exe.Sources.Select(Expand).ToList(), _env.SourcesDirectory).GroupBy(i => Path.GetExtension(i)).ToDictionary(i => i.Key, i => i.ToList());
			Dictionary<SourceType, List<string>> sources = SourceExtensions.ToDictionary(i => i.Key, i => i.Value.SelectMany(j =>
			{
				bool found = groups.TryGetValue(j, out List<string> entries);
				return found ? entries : new List<string>();
			}).ToList());

			//An extension nothing compiles would otherwise be dropped in silence, and the link error names something else
			List<string> unknown = groups.Where(i => !SourceExtensions.Values.Any(j => j.Contains(i.Key))).SelectMany(i => i.Value).ToList();
			if (unknown.Count > 0)
				throw new Exception($"Exe '{_exe.Name}' lists sources UBuild does not compile: {string.Join(", ", unknown)}");

			bool unity = _options.Unity ?? _exe.Unity;
			List<string> batched = new List<string>();

			//C, then C++, then asm: the object order the linker sees
			foreach (SourceType type in new[] { SourceType.C, SourceType.Cpp, SourceType.Asm })
			{
				foreach (string source in sources[type])
				{
					//Get absolute paths
					string input = _env.GetSourcePath(source);
					string objectPath = _env.GetObjectPath(source, _toolchain.Name, _exe.RelPath);

					string compiler = Compiler(type);
					List<string> flags = CompileFlags(type, input, objectPath);

					//Recorded per source even when unity batches them, so editors still resolve each file
					CompileCommands.Add(_env.Directory, input, compiler + " " + string.Join(" ", flags));

					//Only C++ batches: C and asm are usually vendor code full of file statics
					if (unity && type == SourceType.Cpp && !Excluded(source))
					{
						batched.Add(input);
						continue;
					}

					objects.Add(objectPath);
					compiles.Add(new RunTask(compiler, flags));
				}
			}

			if (batched.Count > 0)
				compiles.AddRange(UnityTasks(batched, objects));

			//Created up front because the compiles below run at once and would race to create them
			foreach (string obj in objects)
			{
				string dir = Path.GetDirectoryName(obj);
				if (!Directory.Exists(dir))
					Directory.CreateDirectory(dir);
			}

			//Compiles are independent of each other; everything after depends on all of them.
			if (!Runner.RunAll(compiles, verbose, output))
				return ActionResult.Failed;

			List<ITask> tasks = new List<ITask>();

			//Link
			{
				List<string> flags = new List<string>
				{
					string.Join(" ", objects.Select(Shell.Quote)),
					$"-o {Shell.Quote(OutputFile)}",
				};

				//Add flags
				foreach (string flag in _exe.LinkFlags)
					flags.Add(_exe.Eval(flag));

				flags.AddRange(_toolchain.LinkFlags);
				tasks.Add(new RunTask(_toolchain.Gpp, flags));
			}

			//Post build
			tasks.AddRange(_exe.PostBuild.Select(i => StepTask(i, env)));

			//View created file
			tasks.Add(new RunTask("stat", new List<string> { Shell.Quote(OutputFile) }));

			//Display and execute
			if (!Runner.Run(tasks, verbose, output))
				return ActionResult.Failed;

			output.Line($"\tSuccessfully built {OutputFile}");

			return ActionResult.Success;
		}

		//Generated files that include the sources, one object per batch instead of one per source.
		private List<ITask> UnityTasks(List<string> inputs, List<string> objects)
		{
			//Sorted so a batch is the same set of files from one run to the next
			inputs.Sort(StringComparer.Ordinal);

			//Fixed, so batches do not vary with core count and differ from one machine to the next
			int size = _exe.UnityBatchSize > 0 ? _exe.UnityBatchSize : DefaultBatchSize;

			string dir = Path.Combine(_env.OutputObjectDirectory, _toolchain.Name, _exe.RelPath);
			Directory.CreateDirectory(dir);

			List<ITask> tasks = new List<ITask>();
			for (int start = 0, batch = 0; start < inputs.Count; start += size, batch++)
			{
				List<string> members = inputs.GetRange(start, Math.Min(size, inputs.Count - start));
				CheckMacros(members);

				string source = Path.Combine(dir, $"unity_{batch}.cpp");
				string output = Path.Combine(dir, $"unity_{batch}.o");
				File.WriteAllText(source, string.Concat(members.Select(i => $"#include \"{i}\"\n")));

				objects.Add(output);
				tasks.Add(new RunTask(_toolchain.Gpp, CompileFlags(SourceType.Cpp, source, output)));
			}

			return tasks;
		}

		//Two files disagreeing on a macro silently takes the first, so stop; one file's own #if branches are its business.
		private static void CheckMacros(List<string> members)
		{
			Dictionary<string, KeyValuePair<string, string>> seen = new Dictionary<string, KeyValuePair<string, string>>();

			foreach (string member in members)
			{
				foreach (string line in File.ReadLines(member))
				{
					string text = line.TrimStart();
					if (!text.StartsWith("#define"))
						continue;

					string rest = text.Substring("#define".Length).TrimStart();
					if (rest.Length == 0)
						continue;

					int split = rest.IndexOfAny(new[] { ' ', '\t', '(' });
					string name = split == -1 ? rest : rest.Substring(0, split);
					string value = split == -1 ? string.Empty : rest.Substring(split).Trim();

					if (!seen.TryGetValue(name, out KeyValuePair<string, string> prior))
						seen[name] = new KeyValuePair<string, string>(value, member);
					else if (prior.Key != value && prior.Value != member)
						throw new Exception($"Unity batch defines {name} two ways: {prior.Value} and {member}. Exclude one with UnityExclude.");
				}
			}
		}

		private bool Excluded(string source)
		{
			foreach (string pattern in _exe.UnityExclude)
			{
				if (Matches(source, pattern))
					return true;
			}

			return false;
		}

		//Wildcard match over the source path, where '*' stands for any run of characters
		private static bool Matches(string text, string pattern)
		{
			int t = 0, p = 0, star = -1, mark = 0;

			while (t < text.Length)
			{
				if (p < pattern.Length && (pattern[p] == '?' || pattern[p] == text[t]))
				{
					t++;
					p++;
				}
				else if (p < pattern.Length && pattern[p] == '*')
				{
					star = p++;
					mark = t;
				}
				else if (star != -1)
				{
					p = star + 1;
					t = ++mark;
				}
				else
				{
					return false;
				}
			}

			while (p < pattern.Length && pattern[p] == '*')
				p++;

			return p == pattern.Length;
		}

		private string Compiler(SourceType type)
		{
			switch (type)
			{
				case SourceType.C: return _toolchain.Gcc;
				case SourceType.Cpp: return _toolchain.Gpp;
				case SourceType.Asm: return _toolchain.As;
				default: throw new NotImplementedException();
			}
		}

		//One source's command line; the exe's own includes come before the env-wide ones
		private List<string> CompileFlags(SourceType type, string input, string output)
		{
			List<string> flags = new List<string>();
			if (type == SourceType.Asm)
				flags.Add(_toolchain.AsFlags);

			flags.Add("-c");
			flags.Add(Shell.Quote(input));
			flags.Add($"-o {Shell.Quote(output)}");

			//Assembly is preprocessed but never got the include path
			if (type != SourceType.Asm)
			{
				flags.Add($"-I {Shell.Quote(_env.SourcesDirectory)}");

				//Expanded, so an include root outside the repo can be named by variable rather than by ".."
				foreach (string include in _exe.IncludeDirs)
					flags.Add($"-I {Shell.Quote(Vars.Expand(Expand(include)))}");
				foreach (string include in _env.IncludeDirs)
					flags.Add($"-I {Shell.Quote(Vars.Expand(Expand(include)))}");
			}

			foreach (string define in _exe.Defines)
				flags.Add(Define(define));
			foreach (string define in _env.Defines)
				flags.Add(Define(define));

			flags.AddRange(_exe.Flags);
			if (type == SourceType.Cpp)
				flags.AddRange(_exe.CppFlags);

			flags.AddRange(_toolchain.Flags);
			if (type == SourceType.Cpp)
				flags.AddRange(_toolchain.CppFlags);

			return flags;
		}

		//The exe's own values, then the toolchain's name, which Executable cannot know
		private string Expand(string text)
		{
			return _exe.Eval(text).Replace("$Toolchain", _toolchain.Name);
		}

		//Pre/post build step: "$ToolchainProperty:args", args are evaluated against the exe
		private ITask StepTask(string step, Dictionary<string, string> env)
		{
			//Split once, because a step's arguments legitimately contain colons
			string[] parts = step.Split(':', 2);
			if (parts.Length != 2 || !parts[0].StartsWith('$'))
				throw new Exception($"Build step '{step}' is not \"$ToolchainProperty: args\"");

			string name = parts[0].Substring(1);
			PropertyInfo property = typeof(Toolchain).GetProperty(name);
			if (property == null || property.PropertyType != typeof(string))
				throw new Exception($"Build step '{step}' names ${name}, which is not a toolchain tool. Available: {string.Join(", ", ToolchainTools())}");

			string bin = property.GetValue(_toolchain) as string;
			List<string> args = parts[1].Split(' ', StringSplitOptions.RemoveEmptyEntries).Select(Expand).ToList();
			return new RunTask(bin, args, env);
		}

		private static IEnumerable<string> ToolchainTools()
		{
			return typeof(Toolchain).GetProperties().Where(i => i.PropertyType == typeof(string)).Select(i => "$" + i.Name);
		}

		//Escaped because the runtime re-parses the args and eats bare quotes, losing FOO="bar.h"
		private static string Define(string define)
		{
			return $"-D {define.Replace("\"", "\\\"")}";
		}

		private static List<string> ExpandedSources(List<string> sources, string sourceDir)
		{
			return sources.SelectMany(i =>
			{
				if (!i.Contains('*'))
					return new List<string> { i };

				string search = Path.GetFileName(i);
				string directory = Path.Combine(sourceDir, Path.GetDirectoryName(i));
				if (!Directory.Exists(directory))
					throw new Exception($"Source '{i}' looks in {directory}, which does not exist");

				string[] files = Directory.GetFiles(directory, search);
				if (files.Length == 0)
					throw new Exception($"Source '{i}' matches no file in {directory}");

				//Sorted, because the filesystem's order would otherwise pick the link order and the image with it
				Array.Sort(files, StringComparer.Ordinal);
				return files.Select(j => j.Replace(sourceDir + "/", string.Empty)).ToList();
			//A file caught by a glob and named again would otherwise reach the linker twice
			}).Distinct().ToList();
		}
	}
}
