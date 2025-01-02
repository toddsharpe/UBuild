using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UBuild.Models;
using Environment = UBuild.Models.Environment;

namespace UBuild.Actions
{
	public class BuildProjectAction : IAction
	{
		private readonly List<BuildAction> _builds;

		internal BuildProjectAction(Environment env, Project project)
		{
			_builds = new List<BuildAction>();

			foreach (Project.ExeEntry entry in project.Exes)
			{
				Executable exe = env.GetExe(entry.Name) ?? throw new Exception("Exe not found");

				if (entry.Toolchain == Toolchain.ALL)
				{
					foreach (Toolchain toolchain in env.Toolchains)
					{
						_builds.Add(new BuildAction(env, exe, toolchain));
					}
				}
				else
				{
					Toolchain toolchain = env.Toolchains.Single(i => i.Name == entry.Toolchain);
					_builds.Add(new BuildAction(env, exe, toolchain));
				}
			}
		}
		
		public ActionResult Run(bool verbose)
		{
			foreach (IAction action in _builds)
			{
				if (action.Run(verbose) == ActionResult.Failed)
					return ActionResult.Failed;
			}

			return ActionResult.Success;
		}
	}
}