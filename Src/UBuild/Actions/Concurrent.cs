using System.Threading.Tasks;
using UBuild.Tasks;

namespace UBuild.Actions
{
	internal static class Concurrent
	{
		//Exes share nothing once each generates into its own directory, so they build at once
		internal static ActionResult Run(IList<IAction> builds, bool verbose, Output output, int jobs)
		{
			if (builds.Count == 0)
				return ActionResult.Success;

			//One exe has nothing to interleave with, so it streams as it goes
			if (builds.Count == 1)
				return builds[0].Run(verbose, output);

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
					broken.Add(action.Label);
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
