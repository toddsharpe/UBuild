using UBuild.Actions;
using UBuild.Models;
using Enum = System.Enum;
using Environment = UBuild.Models.Environment;

namespace UBuild.Commands
{
	internal static class Package
	{
		public static int Execute(string project, string file = nameof(PackageType.Zip), bool verbose = false)
			=> Cli.Guard(() => Perform(project, file, verbose));

		private static int Perform(string project, string file, bool verbose)
		{
			Console.WriteLine("Action: Package");

			string envFile = Path.Combine(Directory.GetCurrentDirectory(), Environment.FileName);
			Environment env = Environment.Load(envFile);
			Console.WriteLine($"\tLoaded: {envFile}");
			Console.WriteLine($"\tProject: {project}");

			if (!Enum.TryParse(file, true, out PackageType type))
				throw new Exception($"Unknown package type '{file}', expected one of: {string.Join(", ", Enum.GetNames<PackageType>())}");

			Project target = env.Projects.SingleOrDefault(i => i.Name == project);
			if (target == null)
				throw new Exception("Project not found");

			IAction action = new PackageAction(env, target, type);
			ActionResult result = action.Run(verbose, new UBuild.Tasks.Output(false));
			return result == ActionResult.Failed ? 1 : 0;
		}
	}
}
