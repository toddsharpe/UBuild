using UBuild.Tasks;
using UBuild.Models;
using Environment = UBuild.Models.Environment;

namespace UBuild.Actions
{
	internal class RunScriptAction : IAction
	{
		private readonly Environment _env;
		private readonly Executable _exe;
		private readonly Script _script;
		//An empty Ext says the script yields no single artifact: a Blazor publish is a directory, not a file.
		private bool HasArtifact => !string.IsNullOrEmpty(_script.Ext);
		public string OutputFile => HasArtifact ? Path.Combine(_exe.OutDir, _exe.Name + _script.Ext) : _exe.OutDir;
		public string Label => $"{_exe.Name} ({_script.Name})";
		
		internal RunScriptAction(Environment env, Executable exe, Script script)
		{
			_env = env;
			_exe = exe;
			_script = script;
		}
		
		public ActionResult Run(bool verbose, Output output)
		{
			output.Line($"Building {_exe.Name} for {_script.Name}");

			List<ITask> tasks = [new RunTask(_script.Bash, new List<string> { Shell.Quote(_script.Location), _exe.Name, Shell.Quote(OutputFile) })];

			//Only a script that names an artifact has one to show; the rest are done when the script is.
			if (HasArtifact)
				tasks.Add(new RunTask("stat", new List<string> { Shell.Quote(OutputFile) }));

			if (!Runner.Run(tasks, verbose, output))
				return ActionResult.Failed;

			output.Line($"\tSuccessfully built {OutputFile}");

			return ActionResult.Success;
		}
	}
}
