using System.Text;

namespace UBuild.Tasks
{
	//One exe's console output, held until that exe is done so concurrent builds do not interleave
	internal sealed class Output
	{
		private static readonly object Terminal = new object();

		private readonly StringBuilder _text;
		private readonly object _gate = new object();

		//Unbuffered writes straight through, which is what a lone exe and `run` want
		internal Output(bool buffered)
		{
			_text = buffered ? new StringBuilder() : null;
		}

		internal bool Buffered => _text != null;

		internal void Line(string text)
		{
			if (_text == null)
			{
				Console.WriteLine(text);
				return;
			}

			lock (_gate)
				_text.AppendLine(text);
		}

		//Child output arrives with its own newlines
		internal void Write(string text)
		{
			if (string.IsNullOrEmpty(text))
				return;

			if (_text == null)
			{
				Console.Write(text);
				return;
			}

			lock (_gate)
				_text.Append(text);
		}

		//Written in one piece, so an exe's output reaches the terminal together
		internal void Flush()
		{
			if (_text == null)
				return;

			string text;
			lock (_gate)
			{
				text = _text.ToString();
				_text.Clear();
			}

			if (text.Length == 0)
				return;

			lock (Terminal)
				Console.Write(text);
		}
	}
}
