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
		private readonly int _jobs;
		public string Label => string.Join(", ", _builds.Select(i => i.Label));

		internal BuildAllAction(Environment env, BuildOptions options, string only)
		{
			_jobs = options.Jobs;
			_builds = new List<IAction>();

			//Projects overlap on purpose, and building one exe the same way twice only repeats its pre-build steps
			HashSet<string> seen = new HashSet<string>();
			foreach (Project project in env.Projects)
			{
				foreach ((string Key, IAction Action) step in BuildProjectAction.Plan(env, project, options, only))
				{
					if (seen.Add(step.Key))
						_builds.Add(step.Action);
				}
			}
		}

		public ActionResult Run(bool verbose, Output output)
		{
			output.Line("Building All Projects.");
			return Concurrent.Run(_builds, verbose, output, _jobs);
		}
	}
}
