using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UBuild.Models;
using UBuild.Tasks;
using Environment = UBuild.Models.Environment;

namespace UBuild.Actions
{
	internal class BuildProjectAction : IAction
	{
		private readonly List<IAction> _builds;
		private readonly int _jobs;
		public string Label => string.Join(", ", _builds.Select(i => i.Label));

		internal BuildProjectAction(Environment env, Project project, BuildOptions options, string only)
		{
			_jobs = options.Jobs;
			List<(string Key, IAction Action)> steps = Plan(env, project, options, only);
			if (steps.Count == 0)
				throw new Exception($"Project '{project.Name}' builds nothing with toolchain '{only}'");

			_builds = steps.Select(i => i.Action).ToList();
		}

		//Keyed by exe and by how it is built, so BuildAllAction can drop what an earlier project already covered
		internal static List<(string Key, IAction Action)> Plan(Environment env, Project project, BuildOptions options, string only)
		{
			//A named toolchain narrows the project to its own half, which is what -p with -t has always read as
			bool filtered = only != Toolchain.ALL;
			List<(string Key, IAction Action)> steps = new List<(string, IAction)>();

			foreach (Project.ExeEntry entry in project.Exes ?? new List<Project.ExeEntry>())
			{
				if (String.IsNullOrWhiteSpace(entry.Toolchain) && String.IsNullOrWhiteSpace(entry.Script))
					throw new Exception($"Exe '{entry.Name}' in project '{project.Name}' names neither a Toolchain nor a Script");

				//A script is not a toolchain, so naming one leaves script exes out
				if (String.IsNullOrWhiteSpace(entry.Toolchain))
				{
					if (filtered)
						continue;

					Script script = env.FindScript(entry.Script);
					steps.Add(($"{entry.Name}\0{script.Name}", new RunScriptAction(env, env.RequireExe(entry.Name), script)));
					continue;
				}

				foreach (Toolchain toolchain in Chains(env, entry.Toolchain, only))
					steps.Add(($"{entry.Name}\0{toolchain.Name}", new BuildAction(env, env.RequireExe(entry.Name), toolchain, options)));
			}

			return steps;
		}

		private static IEnumerable<Toolchain> Chains(Environment env, string declared, string only)
		{
			List<Toolchain> chains = declared == Toolchain.ALL ? env.Toolchains : new List<Toolchain> { env.FindToolchain(declared) };
			return only == Toolchain.ALL ? chains : chains.Where(i => i.Name == only);
		}

		public ActionResult Run(bool verbose, Output output)
		{
			return Concurrent.Run(_builds, verbose, output, _jobs);
		}
	}
}
