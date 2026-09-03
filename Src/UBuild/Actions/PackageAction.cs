using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UBuild.Packagers;
using UBuild.Models;
using UBuild.Tasks;
using Environment = UBuild.Models.Environment;

namespace UBuild.Actions
{
	public enum PackageType
	{
		Zip,
		TarGz
	}
	
	internal class PackageAction : IAction
	{
		private readonly Environment _env;
		private readonly Project _project;
		private readonly PackageType _type;
		public string Label => _project.Name;

		internal PackageAction(Environment env, Project project, PackageType type)
		{
			_env = env;
			_project = project;
			_type = type;
		}

		public ActionResult Run(bool verbose, Tasks.Output output)
		{
			Console.WriteLine("Packaging {0} for {1}", _project.Name, _type);

			string packageDir = Path.Combine(_env.OutputExeDirectory, "packages");
			if (!Directory.Exists(packageDir))
				Directory.CreateDirectory(packageDir);

			string destFile = Path.Combine(packageDir, _project.Name);

			using (IPackager packager = GetPackager(destFile))
			{
				//Delete package if it exists
				if (File.Exists(packager.DestFile))
					File.Delete(packager.DestFile);

				//Initialize packager
				packager.Init();

				//Add all the target binaries, resolved the same way BuildProjectAction builds them
				IEnumerable<string> binaries = (_project.Exes ?? new List<Project.ExeEntry>()).SelectMany(entry =>
				{
					Executable target = _env.GetExe(entry.Name) ?? throw new Exception("Exe not found");

					if (entry.Toolchain == Toolchain.ALL)
						return _env.Toolchains.Select(toolchain => Path.Combine(target.OutDir, target.Name + toolchain.Ext));

					if (!String.IsNullOrWhiteSpace(entry.Toolchain))
					{
						Toolchain toolchain = _env.Toolchains.Single(i => i.Name == entry.Toolchain);
						return new[] { Path.Combine(target.OutDir, target.Name + toolchain.Ext) };
					}

					if (!String.IsNullOrWhiteSpace(entry.Script))
					{
						Script script = _env.Scripts.Single(i => i.Name == entry.Script);
						return new[] { Path.Combine(target.OutDir, target.Name + script.Ext) };
					}

					throw new NotImplementedException();
				});
				packager.AddEntries("bin", binaries);

				//Add all the configs
				IEnumerable<string> configs = (_project.Configs ?? new List<string>()).Select(i => Path.Combine(_env.ConfigsDirectory, i));
				packager.AddEntries("dat", configs);

				Console.WriteLine("\tSuccessfully built {0}", packager.DestFile);
			}

			return ActionResult.Success;
		}

		private IPackager GetPackager(string destFile)
		{
			switch (_type)
			{
				case PackageType.Zip:
					return new ZipPackager(destFile);

				case PackageType.TarGz:
					return new TarGzPackager(destFile);

				default:
					throw new Exception("Unknown packager");
			}
		}
	}
}
