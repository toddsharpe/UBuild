﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UBuild.Configs;

namespace UBuild.Configs
{
	internal class TargetFile : ConfigFile
	{
		internal static readonly string FileName = "TARGET";

		public string Name { get; }
		public string BinName { get; set; }
		public string Toolchain { get; set; }
		public List<string> IncludeDirs { get; set; }
		public List<string> AsmSources { get; set; }
		public List<string> CSources { get; set; }
		public List<string> CppSources { get; set; }
		public List<string> Defines { get; set; }
		public List<string> Flags { get; set; }
		public List<string> CppFlags { get; set; }
		public List<string> LinkFlags { get; set; }
		public List<string> PostBuild { get; set; }

		internal TargetFile(string sourceDir, string name) : base(Path.Combine(sourceDir, name, FileName))
		{
			Name = name;
			IncludeDirs ??= new List<string>();
			AsmSources ??= new List<string>();
			CSources ??= new List<string>();
			CppSources ??= new List<string>();
			Defines ??= new List<string>();
			Flags ??= new List<string>();
			CppFlags ??= new List<string>();
			LinkFlags ??= new List<string>();
			PostBuild ??= new List<string>();
		}
	}
}
