using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace UBuild.Packagers
{
	public interface IPackager : IDisposable
	{
		string DestFile { get;}
		void Init();
		void AddEntries(string root, IEnumerable<string> bins);
	}
}