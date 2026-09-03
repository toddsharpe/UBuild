using UBuild.Actions;
using UBuild.Models;
using Environment = UBuild.Models.Environment;

namespace UBuild.Commands
{
	internal static class Build
	{
		public static int Execute(string exe = null, string project = UBuild.Models.Project.ALL, string toolchain = UBuild.Models.Toolchain.ALL, string script = null, int jobs = 0, bool unity = false, bool noUnity = false, bool verbose = false)
			=> Cli.Guard(() => Perform(exe, project, toolchain, script, jobs, unity, noUnity, verbose));

		private static int Perform(string exe, string project, string toolchain, string script, int jobs, bool unity, bool noUnity, bool verbose)
		{
			if (unity && noUnity)
			{
				Console.Error.WriteLine("Cannot pass both --unity and --no-unity");
				return 1;
			}

			if (string.IsNullOrWhiteSpace(exe) && string.IsNullOrWhiteSpace(project))
			{
				Console.Error.WriteLine("Must specify a exe or project");
				return 1;
			}

			Console.WriteLine("Action: Build");

			string envFile = Path.Combine(Directory.GetCurrentDirectory(), Environment.FileName);
			Environment env = Environment.Load(envFile);
			Console.WriteLine($"\tLoaded: {envFile}");

			BuildOptions options = new BuildOptions
			{
				Jobs = jobs > 0 ? jobs : System.Environment.ProcessorCount,
				Unity = unity ? true : noUnity ? false : (bool?)null,
			};
			Console.WriteLine($"\tJobs: {options.Jobs}");
			UBuild.Tasks.Runner.Budget(options.Jobs);

			//Checked once here, so a typo is named rather than reported as a project that builds nothing
			if (toolchain != UBuild.Models.Toolchain.ALL)
				env.FindToolchain(toolchain);

			//The exe is the narrowest thing asked for, so it wins: `project` defaults to ALL and would swallow it.
			IAction action;
			if (!string.IsNullOrWhiteSpace(exe))
			{
				Console.WriteLine($"\tExe: {exe}");

				Executable target = env.RequireExe(exe);
				action = Single(env, exe, target, toolchain, script, options);
			}
			else if (project == UBuild.Models.Project.ALL)
			{
				Console.WriteLine($"\tAll Projects");

				action = new BuildAllAction(env, options, toolchain);
			}
			else
			{
				Console.WriteLine($"\tProject: {project}");
				if (toolchain != UBuild.Models.Toolchain.ALL)
					Console.WriteLine($"\tToolchain: {toolchain}");

				action = new BuildProjectAction(env, env.FindProject(project), options, toolchain);
			}

			ActionResult result = action.Run(verbose, new UBuild.Tasks.Output(false));
			CompileCommands.Write(env.Directory);
			return result == ActionResult.Failed ? 1 : 0;
		}

		//A script builds some exes and a toolchain the rest; the project entries already say which, so a flag is for ambiguity only
		private static IAction Single(Environment env, string exe, Executable target, string toolchain, string script, BuildOptions options)
		{
			if (!string.IsNullOrWhiteSpace(script))
				return new RunScriptAction(env, target, env.FindScript(script));

			if (toolchain != UBuild.Models.Toolchain.ALL)
				return new BuildAction(env, target, env.FindToolchain(toolchain), options);

			string named = Inferred(env, exe);
			Console.WriteLine($"\tBuilt by: {named}");

			if (env.Toolchains.Any(i => i.Name == named))
				return new BuildAction(env, target, env.FindToolchain(named), options);

			return new RunScriptAction(env, target, env.FindScript(named));
		}

		internal static string Inferred(Environment env, string exe)
		{
			List<string> ways = env.WaysToBuild(exe);
			if (ways.Count == 0)
				throw new Exception($"No project builds exe '{exe}', so how to build it is unknown: name a toolchain with -t or a script with -s");
			if (ways.Count > 1)
				throw new Exception($"Exe '{exe}' is built more than one way ({string.Join(", ", ways)}): name one with -t or -s");
			if (ways[0] == UBuild.Models.Toolchain.ALL)
				throw new Exception($"Exe '{exe}' is declared with Toolchain ALL: name one with -t");

			return ways[0];
		}
	}
}
