using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace UBuild.Models
{
	public class Executable
	{
		internal const string FileName = "Exe_build.json";

		public string Name { get; set; }
		//Another exe under Exes whose lists this one is added to; one level, so the chain stays readable
		public string Extends { get; set; }
		public List<string> Sources { get; set; } = new List<string>();
		public List<string> IncludeDirs { get; set; } = new List<string>();
		public List<string> Defines { get; set; } = new List<string>();
		public List<string> Flags { get; set; } = new List<string>();
		public List<string> CppFlags { get; set; } = new List<string>();
		public List<string> LinkFlags { get; set; } = new List<string>();
		public List<string> PreBuild { get; set; } = new List<string>();
		public List<string> PostBuild { get; set; } = new List<string>();

		//Compile C++ sources as generated files that include them; batch size 0 takes the default
		public bool Unity { get; set; }
		public int UnityBatchSize { get; set; }
		public List<string> UnityExclude { get; set; } = new List<string>();

		internal string OutDir { get; set; }
		//Path within Exes (eg "Hosted/MyHosted"); namespaces objects so exes never share one
		internal string RelPath { get; set; }
		internal string BinFile => Path.Combine(OutDir, Name);

		internal static Executable Load(string filename)
		{
			return Config.Config.ReadJson(filename, Config.UBuildJsonContext.Default.Executable);
		}

		//Base first, then the exe's own: lists concatenate, and a scalar takes the derived value when it has one
		internal static Executable Merge(Executable basis, Executable derived)
		{
			return new Executable
			{
				Name = derived.Name ?? basis.Name,
				Sources = Join(basis.Sources, derived.Sources),
				IncludeDirs = Join(basis.IncludeDirs, derived.IncludeDirs),
				Defines = Join(basis.Defines, derived.Defines),
				Flags = Join(basis.Flags, derived.Flags),
				CppFlags = Join(basis.CppFlags, derived.CppFlags),
				LinkFlags = Join(basis.LinkFlags, derived.LinkFlags),
				PreBuild = Join(basis.PreBuild, derived.PreBuild),
				PostBuild = Join(basis.PostBuild, derived.PostBuild),
				Unity = derived.Unity || basis.Unity,
				UnityBatchSize = derived.UnityBatchSize > 0 ? derived.UnityBatchSize : basis.UnityBatchSize,
				UnityExclude = Join(basis.UnityExclude, derived.UnityExclude),
			};
		}

		private static List<string> Join(List<string> first, List<string> second)
		{
			return (first ?? new List<string>()).Concat(second ?? new List<string>()).ToList();
		}

		public string Eval(string expression)
		{
			expression = expression.Replace("$OutDir", OutDir);
			expression = expression.Replace("$BinFile", BinFile);
			expression = expression.Replace("$ExeName", Name);
			return expression;
		}
	}
}
