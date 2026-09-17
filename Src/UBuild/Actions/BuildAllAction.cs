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
	internal class BuildAllAction : IAction
	{
		private readonly List<IAction> _builds;
		private readonly List<ProjectSteps> _steps = new List<ProjectSteps>();
		private readonly int _jobs;
		public string Label => string.Join(", ", _builds.Select(i => i.Label));

		internal BuildAllAction(Environment env, BuildOptions options, string only)
		{
			_jobs = options.Jobs;
			_builds = new List<IAction>();

			//Projects overlap on purpose, and building one exe the same way twice only repeats its pre-build steps
			Dictionary<string, IAction> seen = new Dictionary<string, IAction>();
			foreach (Project project in env.Projects)
			{
				foreach ((string Key, IAction Action) step in BuildProjectAction.Plan(env, project, options, only))
				{
					if (seen.TryAdd(step.Key, step.Action))
						_builds.Add(step.Action);
				}
			}

			//After every project is planned, so an exe a later project shares resolves to the one build that happens
			foreach (Project project in env.Projects)
				_steps.Add(new ProjectSteps(env, project, options, only, key => seen.GetValueOrDefault(key)));
		}

		public ActionResult Run(bool verbose, Output output)
		{
			output.Line("Building All Projects.");
			ActionResult result = Concurrent.Run(_builds, verbose, output, _jobs, out HashSet<IAction> failures);

			//In the order the projects are declared, each only if all of its own exes built
			foreach (ProjectSteps steps in _steps)
			{
				if (steps.Run(failures, verbose, output) == ActionResult.Failed)
					result = ActionResult.Failed;
			}

			return result;
		}
	}
}
