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
		public List<string> Sources { get; set; } = new List<string>();
		public List<string> IncludeDirs { get; set; } = new List<string>();
		public List<string> Defines { get; set; } = new List<string>();
		public List<string> Flags { get; set; } = new List<string>();
		public List<string> CppFlags { get; set; } = new List<string>();
		public List<string> LinkFlags { get; set; } = new List<string>();
		public List<string> PostBuild { get; set; } = new List<string>();

		internal string OutDir { get; set; }
		internal string BinFile => Path.Combine(OutDir, Name);

		internal static Executable Load(string filename)
		{
			return Config.Config.ReadJson<Executable>(filename);
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
