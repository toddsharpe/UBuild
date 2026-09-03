namespace UBuild.Commands
{
	internal static class Cli
	{
		//A missing project or exe is a user error, so it reads as one line rather than a .NET stack trace.
		internal static int Guard(Func<int> body)
		{
			try
			{
				return body();
			}
			catch (Exception ex)
			{
				Console.Error.WriteLine($"Error: {Unwrap(ex).Message}");
				return 1;
			}
		}

		//Parallel compiles surface as an AggregateException, whose own message buries the one that matters
		private static Exception Unwrap(Exception ex)
		{
			return ex is AggregateException aggregate && aggregate.InnerExceptions.Count > 0 ? aggregate.InnerExceptions[0] : ex;
		}
	}
}
