using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace UBuild.Models
{
	public class Script
	{
		public string Name { get; set; }
		public string Location { get; set; }
		public string Ext { get; set; } = ".elf";
		public string Bash => "bash";
	}
}
