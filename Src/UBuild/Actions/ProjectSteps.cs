using UBuild.Models;
using UBuild.Tasks;
using Environment = UBuild.Models.Environment;

namespace UBuild.Actions
{
	//A project's PostBuild, run once every exe it lists has built in this invocation
	internal sealed class ProjectSteps
	{
		private readonly Environment _env;
		private readonly Project _project;
		private readonly List<(string Key, IAction Action)> _built;
		private readonly List<string> _missing;

		//Planned with the toolchain filter and without it: whatever the filter dropped is an exe the project did not get
		internal ProjectSteps(Environment env, Project project, BuildOptions options, string only, Func<string, IAction> resolve)
		{
			_env = env;
			_project = project;
			List<(string Key, IAction Action)> planned = BuildProjectAction.Plan(env, project, options, only);
			_built = planned.Select(i => (i.Key, resolve(i.Key) ?? i.Action)).ToList();

			HashSet<string> keys = planned.Select(i => i.Key).ToHashSet();
			_missing = project.PostBuild.Count == 0
				? new List<string>()
				: BuildProjectAction.Plan(env, project, options, Toolchain.ALL).Where(i => !keys.Contains(i.Key)).Select(i => i.Action.Label).ToList();

			//Checked before anything builds, so a bad step is named rather than found after minutes of compiling
			foreach (string step in project.PostBuild)
				Interpreter(step);
		}

		internal ActionResult Run(HashSet<IAction> failures, bool verbose, Output output)
		{
			if (_project.PostBuild.Count == 0)
				return ActionResult.Skipped;

			if (_missing.Count > 0)
			{
				output.Line($"Skipping PostBuild for {_project.Name}: not built here: {string.Join(", ", _missing)}");
				return ActionResult.Skipped;
			}

			List<string> broken = _built.Where(i => failures.Contains(i.Action)).Select(i => i.Action.Label).ToList();
			if (broken.Count > 0)
			{
				output.Line($"Skipping PostBuild for {_project.Name}: failed: {string.Join(", ", broken)}");
				return ActionResult.Skipped;
			}

			output.Line($"PostBuild for {_project.Name}");
			Dictionary<string, string> vars = new Dictionary<string, string>
			{
				{ "Project", _project.Name },
				//name:toolchain, or name:script for an exe a script builds; a key is the two joined by a NUL
				{ "Exes", string.Join(" ", _built.Select(i => i.Key.Replace('\0', ':'))) },
				{ "OutputExeDir", _env.OutputExeDirectory },
			};

			List<ITask> tasks = _project.PostBuild.Select(step =>
			{
				string[] parts = step.Split(':', 2);
				return (ITask)new RunTask(Interpreter(step), parts[1].Split(' ', StringSplitOptions.RemoveEmptyEntries).ToList(), vars);
			}).ToList();

			return Runner.Run(tasks, verbose, output) ? ActionResult.Success : ActionResult.Failed;
		}

		//A project has no one toolchain, so only the interpreters every toolchain shares can run its steps
		private string Interpreter(string step)
		{
			string[] parts = step.Split(':', 2);
			string name = parts.Length == 2 ? parts[0].Trim() : null;
			return name switch
			{
				"$Bash" => "bash",
				"$Python" => "python3",
				_ => throw new Exception($"PostBuild step '{step}' in project '{_project.Name}' is not \"$Bash: args\" or \"$Python: args\""),
			};
		}
	}
}
