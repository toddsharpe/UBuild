using System.CommandLine;
using UBuild.Commands;
using UBuild.Actions;
using UBuild.Models;

public class Program
{
	public static int Main(string[] args)
	{
		Option<string> exe = new Option<string>("--exe", "-e") { Description = "Exe to build." };
		Option<string> project = new Option<string>("--project", "-p") { Description = "Project to build.", DefaultValueFactory = i => Project.ALL };
		Option<string> toolchain = new Option<string>("--toolchain", "-t") { Description = "Toolchain to use.", DefaultValueFactory = i => Toolchain.ALL };
		Option<string> script = new Option<string>("--script", "-s") { Description = "Script that builds the exe, where a script builds it rather than a toolchain." };
		Option<string> arguments = new Option<string>("--args", "-a") { Description = "Arguments passed to the program." };
		Option<string> file = new Option<string>("--file", "-f") { Description = "Package type.", DefaultValueFactory = i => nameof(PackageType.Zip) };
		Option<int> jobs = new Option<int>("--jobs", "-j") { Description = "Compiles to run at once (0 uses the processor count)." };
		Option<bool> unity = new Option<bool>("--unity") { Description = "Force unity builds on, whatever the exe asks for." };
		Option<bool> noUnity = new Option<bool>("--no-unity") { Description = "Force unity builds off, whatever the exe asks for." };
		Option<bool> verbose = new Option<bool>("--verbose", "-v") { Description = "Display commands." };

		Command build = new Command("build", "Build a project or exe.") { exe, project, toolchain, script, jobs, unity, noUnity, verbose };
		build.SetAction(i => Build.Execute(i.GetValue(exe), i.GetValue(project), i.GetValue(toolchain), i.GetValue(script), i.GetValue(jobs), i.GetValue(unity), i.GetValue(noUnity), i.GetValue(verbose)));

		Command run = new Command("run", "Build an exe and run it.") { exe, toolchain, arguments, jobs, unity, noUnity, verbose };
		run.SetAction(i => Run.Execute(i.GetValue(exe), i.GetValue(toolchain), i.GetValue(arguments), i.GetValue(jobs), i.GetValue(unity), i.GetValue(noUnity), i.GetValue(verbose)));

		Command package = new Command("package", "Package a project's binaries and configs.") { project, file, verbose };
		package.SetAction(i => Package.Execute(i.GetValue(project), i.GetValue(file), i.GetValue(verbose)));

		Command clean = new Command("clean", "Delete the object and exe output directories.");
		clean.SetAction(i => Clean.Execute());

		Command list = new Command("list", "List the toolchains, scripts and projects this environment defines.");
		list.SetAction(i => Listing.Execute());

		RootCommand root = new RootCommand("Very simple build system for small projects.");
		foreach (Command command in new[] { build, run, package, clean, list })
			root.Subcommands.Add(command);

		return root.Parse(args).Invoke();
	}
}
