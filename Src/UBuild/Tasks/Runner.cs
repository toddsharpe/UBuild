using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace UBuild.Tasks
{
	internal static class Runner
	{
		//Every launched process takes a slot, so exes running at once cannot oversubscribe past --jobs
		private static SemaphoreSlim _slots = new SemaphoreSlim(System.Environment.ProcessorCount);

		internal static void Budget(int jobs)
		{
			_slots = new SemaphoreSlim(Math.Max(1, jobs));
		}

		//Stops at the first failure, for steps that only make sense in order
		internal static bool Run(IEnumerable<ITask> tasks, bool verbose, Output output)
		{
			foreach (ITask task in tasks)
			{
				if (!Execute(task, verbose, output))
					return false;
			}

			return true;
		}

		//Independent of each other, so all of them run and every failure is reported rather than a sample
		internal static bool RunAll(IList<ITask> tasks, bool verbose, Output output)
		{
			if (tasks.Count <= 1)
				return Run(tasks, verbose, output);

			int failed = 0;
			Parallel.ForEach(tasks, task =>
			{
				if (!Execute(task, verbose, output))
					Interlocked.Increment(ref failed);
			});

			return failed == 0;
		}

		private static bool Execute(ITask task, bool verbose, Output output)
		{
			if (verbose)
				task.Display(output);

			_slots.Wait();
			try
			{
				return task.Run(output);
			}
			finally
			{
				_slots.Release();
			}
		}
	}
}
