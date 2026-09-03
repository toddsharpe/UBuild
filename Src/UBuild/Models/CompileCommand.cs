using System.Text.Json.Serialization;

namespace UBuild.Models
{
	//One entry of a clang compilation database; the key names are what the format requires.
	public class CompileCommand
	{
		[JsonPropertyName("directory")]
		public string Directory { get; set; }

		[JsonPropertyName("file")]
		public string File { get; set; }

		[JsonPropertyName("command")]
		public string Command { get; set; }
	}
}
