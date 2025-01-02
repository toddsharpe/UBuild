using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace UBuild.Models
{
	public class Project
	{
		internal const string ALL = "ALL";

		public class ExeEntry
		{
			public string Name { get; set; }
			public string Toolchain { get; set; }
		}
		public string Name { get; set; }
		public List<ExeEntry> Exes { get; set; }
		public List<string> Configs { get; set; }
	}
}
