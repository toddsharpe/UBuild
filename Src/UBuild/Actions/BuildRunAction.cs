using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using UBuild.Models;
using Environment = UBuild.Models.Environment;

namespace UBuild.Actions
{
	public class BuildRunAction : IAction
	{
		private readonly BuildAction _action;

		internal BuildRunAction(Environment env, Executable exe, Toolchain toolchain)
		{
			_action = new BuildAction(env, exe, toolchain);
		}

		public ActionResult Run(bool verbose)
		{
			ActionResult result = _action.Run(verbose);
			if (result != ActionResult.Success)
				return result;

			Console.WriteLine("---Run---");
			Process process = Process.Start(_action.OutputFile);
			process.WaitForExit();

			return ActionResult.Success;
		}
	}
}
