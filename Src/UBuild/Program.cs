using System;
using System.ComponentModel.DataAnnotations;
using McMaster.Extensions.CommandLineUtils;
using UBuild.Actions;
using UBuild.Models;
using Environment = UBuild.Models.Environment;

public class Program
{
	public static int Main(string[] args) => CommandLineApplication.Execute<Program>(args);

	public enum ActionType
	{
		None,
		Build,
		Package,
		Run
	}

	[Argument(0, Description = "Action")]
	public ActionType Action { get; }

	[Option("-e|--exe <path>", Description = "Exe to build")]
	public string Exe { get;}

	[Option("-p|--project <path>", Description = "Project to build")]
	public string Project { get;} = UBuild.Models.Project.ALL;

	[Option("-t|--toolchain <path>", Description = "Toolchain to use")]
	public string Toolchain { get; } = UBuild.Models.Toolchain.ALL;

	[Option("-f|--file <path>", Description = "Package file")]
	public PackageType PackageType { get;} = PackageType.Zip;

	[Option("-v|--verbose", Description = "Display commands.")]
	public bool Verbose { get;} = false;

	private ValidationResult OnValidate()
	{
		if (Action == ActionType.None)
			return new ValidationResult("Must specify an action");

		switch (Action)
		{
		case ActionType.Build:
			if (String.IsNullOrWhiteSpace(Exe) && String.IsNullOrWhiteSpace(Project))
				return new ValidationResult("Must specify a exe or project");
			break;

		case ActionType.Package:
			if (String.IsNullOrWhiteSpace(Project))
				return new ValidationResult("Must specify a project");
			break;

		case ActionType.Run:
			if (String.IsNullOrWhiteSpace(Exe))
				return new ValidationResult("Must specify an exe");
			break;
		}

		return ValidationResult.Success;
	}

	private int OnExecute()
	{
		Console.WriteLine($"Action: {Action}");

		string envFile = Path.Combine(Directory.GetCurrentDirectory(), Environment.FileName);
		Environment env = Environment.Load(envFile);
		Console.WriteLine($"\tLoaded: {envFile}");

		//Load project, exe, toolchain
		Executable exe = env.GetExe(Exe);
		Project project = env.Projects.SingleOrDefault(i => i.Name == Project);
		Toolchain toolchain = env.Toolchains.SingleOrDefault(i => i.Name == Toolchain);

		IAction action;
		switch (Action)
		{
			case ActionType.Build:
			{
				if (Project == UBuild.Models.Project.ALL)
				{
					Console.WriteLine($"\tAll Projects");

					action = new BuildAllAction(env);
				}
				else if (!string.IsNullOrEmpty(Project))
				{
					Console.WriteLine($"\tProject: {Project}");

					if (project == null)
						throw new Exception("Project not found");
					action = new BuildProjectAction(env, project);
				}
				else
				{
					Console.WriteLine($"\tExe: {Exe}");
					if (exe == null)
						throw new Exception("Exe not found");
					if (toolchain == null)
						throw new Exception("Toolchain not found");
					action = new BuildAction(env, exe, toolchain);
				}
				break;
			}

			case ActionType.Package:
			{
				Console.WriteLine($"\tProject: {Project}");
				if (project == null)
						throw new Exception("Project not found");
				action = new PackageAction(env, project, PackageType);
				break;
			}

			case ActionType.Run:
			{
				Console.WriteLine($"\tExe: {Exe}");
				if (exe == null)
						throw new Exception("Exe not found");
				if (toolchain == null)
						throw new Exception("Toolchain not found");
				action = new BuildRunAction(env, exe, toolchain);
				break;
			}

			default:
				throw new NotImplementedException();
		}

		ActionResult result = action.Run(Verbose);
		return result == ActionResult.Failed ? 1 : 0;
	}
}