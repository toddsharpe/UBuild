using System.Text.Json;
using System.Text.Json.Serialization;

namespace UBuild.Config
{
	public static class Config
	{
		public static T ReadJson<T>(string filename)
		{
			string[] lines = File.ReadAllLines(filename);
			var filtered = lines.Select(FilterComment);
			string contents = string.Join(Environment.NewLine, filtered);
			return JsonSerializer.Deserialize<T>(contents);
		}

		private static string FilterComment(string line)
		{
			int index = line.IndexOf("#");
			if (index == -1)
				return line;
			else
				return line.Substring(0, index);
		}
	}
}
