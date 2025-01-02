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

		private static readonly Dictionary<SourceType, List<string>> SourceExtensions = new Dictionary<SourceType, List<string>>
		{
			{ SourceType.C, new List<string> { ".c" } },
			{ SourceType.Cpp, new List<string> { ".cc", ".cpp" } },
			{ SourceType.Asm, new List<string> { ".s" } },
		};

		private readonly Environment _env;
		private readonly Executable _exe;
		private readonly Toolchain _toolchain;
		public string OutputFile => Path.Combine(_exe.OutDir, _exe.Name + _toolchain.Ext);

		internal BuildAction(Environment env, Executable exe, Toolchain toolchain)
		{
			_env = env;
			_exe = exe;
			_toolchain = toolchain;
		}

		public ActionResult Run(bool verbose)
		{
			Console.WriteLine("Building {0} for {1}", _exe.Name, _toolchain.Name);

			List<ITask> tasks = new List<ITask>();
			List<string> objects = new List<string>();

			//Group files by type
			Dictionary<string, List<string>> groups = ExpandedSources(_exe.Sources, _env.SourcesDirectory).GroupBy(i => Path.GetExtension(i)).ToDictionary(i => i.Key, i => i.ToList());
			Dictionary<SourceType, List<string>> sources = SourceExtensions.ToDictionary(i => i.Key, i => i.Value.SelectMany(j =>
			{
				bool found = groups.TryGetValue(j, out List<string> entries);
				return found ? entries : new List<string>();
			}).ToList());

			//Build C sources
			foreach (string source in sources[SourceType.C])
			{
				//Get absolute paths
				string input = _env.GetSourcePath(source);
				string output = _env.GetObjectPath(source);
				objects.Add(output);

				List<string> flags = new List<string>
				{
					"-c",
					input,
					$"-o {output}",
					$"-I {_env.SourcesDirectory}"
				};

				//Add include dirs
				foreach (string include in _exe.IncludeDirs)
					flags.Add($"-I {include}");

				//Add defines
				foreach (string define in _exe.Defines)
					flags.Add($"-D {define}");

				//Add flags
				flags.AddRange(_exe.Flags);
				flags.AddRange(_toolchain.Flags);

				tasks.Add(new RunTask(_toolchain.Gcc, flags));
			}

			//Build Cpp sources
			foreach (string source in sources[SourceType.Cpp])
			{
				//Get absolute paths
				string input = _env.GetSourcePath(source);
				string output = _env.GetObjectPath(source);
				objects.Add(output);

				List<string> flags = new List<string>
				{
					"-c",
					input,
					$"-o {output}",
					$"-I {_env.SourcesDirectory}"
				};

				//Add include dirs
				foreach (string include in _exe.IncludeDirs)
					flags.Add($"-I {include}");

				//Add defines
				foreach (string define in _exe.Defines)
					flags.Add($"-D {define}");

				//Add flags
				flags.AddRange(_exe.Flags);
				flags.AddRange(_exe.CppFlags);
				flags.AddRange(_toolchain.Flags);
				flags.AddRange(_toolchain.CppFlags);

				tasks.Add(new RunTask(_toolchain.Gpp, flags));
			}

			//Asm sources
			foreach (string source in sources[SourceType.Asm])
			{
				//Get absolute paths
				string input = _env.GetSourcePath(source);
				string output = _env.GetObjectPath(source);
				objects.Add(output);

				List<string> flags = new List<string>
				{
					_toolchain.AsFlags,
					"-c",
					input,
					$"-o {output}",
				};

				//Add defines
				foreach (string define in _exe.Defines)
					flags.Add($"-D {define}");

				//Add flags
				flags.AddRange(_exe.Flags);
				flags.AddRange(_toolchain.Flags);

				tasks.Add(new RunTask(_toolchain.As, flags));
			}

			//Link
			{
				List<string> flags = new List<string>
				{
					string.Join(" ", objects),
					$"-o {OutputFile}",
				};

				//Add flags
				foreach (string flag in _exe.LinkFlags)
					flags.Add(_exe.Eval(flag));

				flags.AddRange(_toolchain.LinkFlags);
				tasks.Add(new RunTask(_toolchain.Gpp, flags));
			}

			//Post build env
			Dictionary<string, string> env = new Dictionary<string, string>
			{
				{ "BinFile", _exe.BinFile },
				{ "OutDir", _exe.OutDir },
			};
			foreach (PropertyInfo property in typeof(Toolchain).GetProperties(BindingFlags.GetProperty | BindingFlags.Public | BindingFlags.Instance))
			{
				if (property.PropertyType != typeof(string))
					continue;
				
				string value = property.GetValue(_toolchain) as string;
				env.Add(property.Name, value);
			}

			//Post build
			foreach (string postbuild in _exe.PostBuild)
			{
				string[] parts = postbuild.Split(':');
				Trace.Assert(parts.Length == 2);
				Trace.Assert(parts[0][0] == '$');
				string bin = typeof(Toolchain).GetProperty(parts[0].Substring(1)).GetValue(_toolchain) as string;
				List<string> args = parts[1].Split(' ').Select(_exe.Eval).ToList();
				tasks.Add(new RunTask(bin, args, env));
			}

			//Create output directories
			foreach (string obj in objects)
			{
				string dir = Path.GetDirectoryName(obj);
				if (!Directory.Exists(dir))
					Directory.CreateDirectory(dir);
			}
			Directory.CreateDirectory(Path.GetDirectoryName(OutputFile));

			//View created file
			tasks.Add(new RunTask("stat", new List<string> { OutputFile }));

			//Display and execute
			foreach (ITask task in tasks)
			{
				if (verbose)
					task.Display();
				if (!task.Run())
					return ActionResult.Failed;
			}

			Console.WriteLine("\tSuccessfully built {0}", OutputFile);

			return ActionResult.Success;
		}

		private static List<string> ExpandedSources(List<string> sources, string sourceDir)
		{
			return sources.SelectMany(i =>
			{
				if (i.Contains('*'))
				{
					string path = Path.GetDirectoryName(i);
					string search = Path.GetFileName(i);
					string[] files = Directory.GetFiles(Path.Combine(sourceDir, path), search);
					return files.Select(i => i.Replace(sourceDir + "/", string.Empty)).ToList();
				}
				else
				{
					return new List<string>{i};
				}
			}).ToList();
		}
	}
}
