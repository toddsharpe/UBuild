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
		ActionResult Run(bool verbose);
	}
}
