using UBuild.Models;
using Environment = UBuild.Models.Environment;

namespace UBuild.Commands
{
	internal static class Listing
	{
		public static int Execute() => Cli.Guard(Perform);

		private static int Perform()
		{
			string envFile = Path.Combine(Directory.GetCurrentDirectory(), Environment.FileName);
			Environment env = Environment.Load(envFile);
			Console.WriteLine($"Loaded: {envFile}");

			Console.WriteLine();
			Console.WriteLine("Toolchains:");
			foreach (Toolchain toolchain in env.Toolchains)
				Console.WriteLine($"\t{toolchain.Name}\t{toolchain.Gpp} ({toolchain.Ext})");

			if (env.Scripts.Count > 0)
			{
				Console.WriteLine();
				Console.WriteLine("Scripts:");
				foreach (Script script in env.Scripts)
					Console.WriteLine($"\t{script.Name}\t{script.Location}");
			}

			Console.WriteLine();
			Console.WriteLine("Projects:");
			foreach (Project project in env.Projects)
			{
				Console.WriteLine($"\t{project.Name}");
				foreach (Project.ExeEntry entry in project.Exes ?? new List<Project.ExeEntry>())
				{
					string via = !string.IsNullOrWhiteSpace(entry.Toolchain) ? entry.Toolchain : entry.Script;
					Console.WriteLine($"\t\t{entry.Name} [{via}]");
				}
			}

			return 0;
		}
	}
}
