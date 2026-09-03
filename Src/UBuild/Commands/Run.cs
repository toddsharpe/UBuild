using UBuild.Actions;
using UBuild.Models;
using Environment = UBuild.Models.Environment;

namespace UBuild.Commands
{
	internal static class Run
	{
		public static int Execute(string exe, string toolchain = UBuild.Models.Toolchain.ALL, string args = null, int jobs = 0, bool unity = false, bool noUnity = false, bool verbose = false)
			=> Cli.Guard(() => Perform(exe, toolchain, args, jobs, unity, noUnity, verbose));

		private static int Perform(string exe, string toolchain, string args, int jobs, bool unity, bool noUnity, bool verbose)
		{
			Console.WriteLine("Action: Run");
			Console.WriteLine($"\tExe: {exe}");

			string envFile = Path.Combine(Directory.GetCurrentDirectory(), Environment.FileName);
			Environment env = Environment.Load(envFile);
			Console.WriteLine($"\tLoaded: {envFile}");

			Executable target = env.RequireExe(exe);

			//As with build, the project entries already name the toolchain, so -t is for ambiguity only
			Toolchain chain = env.FindToolchain(toolchain == UBuild.Models.Toolchain.ALL ? Build.Inferred(env, exe) : toolchain);
			Console.WriteLine($"\tToolchain: {chain.Name}");

			BuildOptions options = new BuildOptions
			{
				Jobs = jobs > 0 ? jobs : System.Environment.ProcessorCount,
				Unity = unity ? true : noUnity ? false : (bool?)null,
			};

			IAction action = new BuildRunAction(env, target, chain, options, args);
			UBuild.Tasks.Runner.Budget(options.Jobs);
			ActionResult result = action.Run(verbose, new UBuild.Tasks.Output(false));
			CompileCommands.Write(env.Directory);
			return result == ActionResult.Failed ? 1 : 0;
		}
	}
}
