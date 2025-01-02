using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UBuild.Packagers;
using UBuild.Models;
using Environment = UBuild.Models.Environment;

namespace UBuild.Actions
{
	public enum PackageType
	{
		Zip
	}
	
	public class PackageAction : IAction
	{
		private readonly Environment _env;
		private readonly Project _project;
		private readonly PackageType _type;

		internal PackageAction(Environment env, Project project, PackageType type)
		{
			_env = env;
			_project = project;
			_type = type;
		}

		public ActionResult Run(bool verbose)
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

				//Add all the target binaries
				IEnumerable<string> binaries = _project.Exes.Select(i =>
				{
					Executable target = _env.GetExe(i.Name);
					return target.Name;
				});
				packager.AddEntries("bin", binaries);

				//Add all the configs
				IEnumerable<string> configs = _project.Configs.Select(i => Path.Combine(_env.ConfigsDirectory, i));
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

				default:
					throw new Exception("Unknown packager");
			}
		}
	}
}
