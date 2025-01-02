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
		private readonly List<BuildProjectAction> _builds;

		internal BuildAllAction(Environment env)
		{
			_builds = new List<BuildProjectAction>();

			foreach (Project project in env.Projects)
			{
				_builds.Add(new BuildProjectAction(env, project));
			}
		}

		public ActionResult Run(bool verbose)
		{
			Console.WriteLine("Building All Projects.");

			foreach (IAction action in _builds)
			{
				if (action.Run(verbose) == ActionResult.Failed)
					return ActionResult.Failed;
			}

			return ActionResult.Success;
		}
	}
}
