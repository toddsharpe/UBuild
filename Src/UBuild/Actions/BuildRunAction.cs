using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using UBuild.Models;
using UBuild.Tasks;
using Environment = UBuild.Models.Environment;

namespace UBuild.Actions
{
	internal class BuildRunAction : IAction
	{
		private readonly BuildAction _action;
		private readonly string _args;
		public string Label => _action.Label;

		internal BuildRunAction(Environment env, Executable exe, Toolchain toolchain, BuildOptions options, string args)
		{
			_action = new BuildAction(env, exe, toolchain, options);
			_args = args;
		}

		public ActionResult Run(bool verbose, Output output)
		{
			ActionResult result = _action.Run(verbose, output);
			if (result != ActionResult.Success)
				return result;

			output.Line("---Run---");
			output.Flush();

			ProcessStartInfo startInfo = new ProcessStartInfo(_action.OutputFile);
			if (!string.IsNullOrWhiteSpace(_args))
				startInfo.Arguments = _args;

			Process process = Process.Start(startInfo);
			process.WaitForExit();

			//The program's own result, so `run` can gate a test suite rather than only its build
			if (process.ExitCode != 0)
			{
				Console.Error.WriteLine($"Error: {_action.OutputFile} exited {process.ExitCode}");
				return ActionResult.Failed;
			}

			return ActionResult.Success;
		}
	}
}
