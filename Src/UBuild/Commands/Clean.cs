using Environment = UBuild.Models.Environment;

namespace UBuild.Commands
{
	internal static class Clean
	{
		public static int Execute() => Cli.Guard(Perform);

		private static int Perform()
		{
			Console.WriteLine("Action: Clean");

			string envFile = Path.Combine(Directory.GetCurrentDirectory(), Environment.FileName);
			Environment env = Environment.Load(envFile);
			Console.WriteLine($"\tLoaded: {envFile}");

			foreach (string dir in new[] { env.OutputObjectDirectory, env.OutputExeDirectory })
			{
				if (Directory.Exists(dir))
				{
					Directory.Delete(dir, true);
					Console.WriteLine($"\tRemoved {dir}");
				}
				else
				{
					Console.WriteLine($"\tSkipped {dir}");
				}
			}

			//The database accumulates across builds, so clean is what clears a stale entry out of it
			string database = Path.Combine(env.Directory, CompileCommands.FileName);
			if (File.Exists(database))
			{
				File.Delete(database);
				Console.WriteLine($"\tRemoved {database}");
			}

			return 0;
		}
	}
}
