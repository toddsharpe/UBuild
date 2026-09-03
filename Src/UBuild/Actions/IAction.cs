using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UBuild.Actions
{
	public enum ActionResult
	{
		Success,
		Skipped,
		Failed
	}

	internal interface IAction
	{
		//How a summary names this one when several ran at once
		string Label { get; }

		ActionResult Run(bool verbose, Tasks.Output output);
	}
}
