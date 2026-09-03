using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace UBuild.Tasks
{
	internal class RunTask : ITask
	{
		private readonly string _bin;
		private readonly List<string> _args;
		private Dictionary<string, string> _env;
		//Args reach the tool as one re-parsed string unless this says otherwise; a shell line has to survive whole
		private readonly bool _literal;

		public RunTask(string bin, List<string> args, Dictionary<string, string> env = null, bool literal = false)
		{
			_bin = bin;
			_args = args;
			_env = env;
			_literal = literal;
		}

		public bool Run(Output output)
		{
			ProcessStartInfo startInfo = new ProcessStartInfo(_bin);
			if (_args != null && _literal)
			{
				foreach (string arg in _args)
					startInfo.ArgumentList.Add(arg);
			}
			else if (_args != null)
			{
				startInfo.Arguments = string.Join(" ", _args);
			}

			if (_env != null)
			{
				foreach (var item in _env)
				{
					//Assigned, not added: OutDir and Name are names the caller's environment may already hold
					startInfo.EnvironmentVariables[item.Key] = item.Value;
				}
			}

			//Inherited when nothing is buffering, so a lone build and `run` still stream as they go
			startInfo.RedirectStandardOutput = output.Buffered;
			startInfo.RedirectStandardError = output.Buffered;

			Process process;
			try
			{
				process = Process.Start(startInfo);
			}
			catch (System.ComponentModel.Win32Exception ex)
			{
				//A cross compiler that is not installed is the common case, and a stack trace says nothing about it
				throw new Exception($"Cannot run '{_bin}': {ex.Message}");
			}

			if (output.Buffered)
			{
				//Both read before the wait, or a full pipe stops the child and the wait never returns
				System.Threading.Tasks.Task<string> outText = process.StandardOutput.ReadToEndAsync();
				System.Threading.Tasks.Task<string> errText = process.StandardError.ReadToEndAsync();
				process.WaitForExit();
				output.Write(outText.Result);
				output.Write(errText.Result);
			}
			else
			{
				process.WaitForExit();
			}

			return process.ExitCode == 0;
		}

		public void Display(Output output)
		{
			output.Line($"{_bin} {(_args != null ? string.Join(" ", _args) : "<none>")}");
			if (_env == null)
				return;

			foreach (var item in _env)
				output.Line($"\t{item.Key}: {item.Value}");
		}
	}
}
