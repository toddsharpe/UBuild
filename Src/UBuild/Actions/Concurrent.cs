using System.Threading.Tasks;
using UBuild.Tasks;

namespace UBuild.Actions
{
	internal static class Concurrent
	{
		//Exes share nothing once each generates into its own directory, so they build at once
		internal static ActionResult Run(IList<IAction> builds, bool verbose, Output output, int jobs)
		{
			return Run(builds, verbose, output, jobs, out _);
		}

		//The failures by action as well, so a project can tell whether every one of its own exes built
		internal static ActionResult Run(IList<IAction> builds, bool verbose, Output output, int jobs, out HashSet<IAction> failures)
		{
			failures = new HashSet<IAction>();
			if (builds.Count == 0)
				return ActionResult.Success;

			//One exe has nothing to interleave with, so it streams as it goes
			if (builds.Count == 1)
			{
				ActionResult result = builds[0].Run(verbose, output);
				if (result == ActionResult.Failed)
					failures.Add(builds[0]);
				return result;
			}

			HashSet<IAction> dropped = failures;
			List<string> broken = new List<string>();
			ParallelOptions options = new ParallelOptions { MaxDegreeOfParallelism = Math.Max(1, jobs) };

			Parallel.ForEach(builds, options, action =>
			{
				//Each collects its own output and hands it over whole, so two failing at once stay readable
				Output own = new Output(true);
				bool failed;

				try
				{
					failed = action.Run(verbose, own) == ActionResult.Failed;
				}
				catch (Exception ex)
				{
					own.Line($"Error: {ex.Message}");
					failed = true;
				}

				own.Flush();
				if (!failed)
					return;

				lock (broken)
				{
					broken.Add(action.Label);
					dropped.Add(action);
				}
			});

			if (broken.Count == 0)
				return ActionResult.Success;

			//Named, because the question a firmware build answers is which boards broke
			broken.Sort(StringComparer.Ordinal);
			output.Line($"Failed ({broken.Count} of {builds.Count}): {string.Join(", ", broken)}");
			return ActionResult.Failed;
		}
	}
}
