using System.Text;

namespace UBuild
{
	internal static class Vars
	{
		//${VAR} or ${VAR:-fallback} from the environment, so a toolchain can sit where it was put
		internal static string Expand(string value)
		{
			if (string.IsNullOrEmpty(value))
				return value;

			StringBuilder text = new StringBuilder();
			for (int i = 0; i < value.Length; i++)
			{
				int end = value[i] == '$' && i + 1 < value.Length && value[i + 1] == '{' ? value.IndexOf('}', i + 2) : -1;
				if (end == -1)
				{
					text.Append(value[i]);
					continue;
				}

				string body = value.Substring(i + 2, end - i - 2);
				int fallback = body.IndexOf(":-");
				string name = fallback == -1 ? body : body.Substring(0, fallback);

				//Unset with no fallback stays literal, so the failure names the variable rather than showing a gap
				string set = System.Environment.GetEnvironmentVariable(name);
				string literal = fallback == -1 ? value.Substring(i, end - i + 1) : body.Substring(fallback + 2);
				text.Append(string.IsNullOrEmpty(set) ? literal : set);
				i = end;
			}

			return text.ToString();
		}
	}
}
