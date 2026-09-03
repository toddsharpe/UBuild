using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace UBuild.Config
{
	public static class Config
	{
		public static T ReadJson<T>(string filename, JsonTypeInfo<T> typeInfo)
		{
			if (!File.Exists(filename))
				throw new Exception($"No {Path.GetFileName(filename)} in {Path.GetDirectoryName(filename)}");

			string[] lines = File.ReadAllLines(filename);
			var filtered = lines.Select(FilterComment);
			//Comments are blanked, not dropped, so a parse error's line number is the one in the file
			string contents = string.Join(Environment.NewLine, filtered);

			T parsed;
			try
			{
				parsed = JsonSerializer.Deserialize(contents, typeInfo);
			}
			catch (JsonException ex)
			{
				throw new Exception($"{filename}: {ex.Message}");
			}

			return parsed ?? throw new Exception($"{filename} holds no object");
		}

		//A '#' inside a JSON string is data, not a comment: defines and paths legitimately contain them.
		private static string FilterComment(string line)
		{
			bool quoted = false;
			for (int i = 0; i < line.Length; i++)
			{
				char c = line[i];

				if (quoted && c == '\\')
				{
					i++;
					continue;
				}

				if (c == '"')
					quoted = !quoted;
				else if (c == '#' && !quoted)
					return line.Substring(0, i);
			}

			return line;
		}
	}
}
