using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace UBuild.Models
{
	public class Toolchain
	{
		internal const string ALL = "ALL";

		public string Name { get; set; }
		public string Bin { get; set; }
		public string Prefix { get; set; }
		public List<string> Flags { get; set; } = new List<string>();
		public List<string> CppFlags { get; set; } = new List<string>();
		public List<string> LinkFlags { get; set; } = new List<string>();
		
		public string Gcc => Path.Combine(Bin, Prefix + "gcc");
 		public string Gpp => Path.Combine(Bin, Prefix + "g++");
		public string As => Path.Combine(Bin, Prefix + "gcc"); /*  -x assembler-with-cpp */
		public string AsFlags => "-x assembler-with-cpp";
		public string ObjCopy => Path.Combine(Bin, Prefix + "objcopy");
		public string ObjDump => Path.Combine(Bin, Prefix + "objdump");
		public string Size => Path.Combine(Bin, Prefix + "size");
		public string Stat => "stat";
		public string Hexdump => "hexdump";
		public string Bash => "bash";
		public string Ext => ".elf";
	}
}