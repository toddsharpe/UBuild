namespace UBuild.Actions
{
	//How a build runs, as opposed to what it builds.
	internal class BuildOptions
	{
		internal int Jobs { get; set; } = 1;

		//Null leaves the choice to each exe; the command line can force it either way.
		internal bool? Unity { get; set; }
	}
}
