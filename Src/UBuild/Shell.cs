namespace UBuild
{
	internal static class Shell
	{
		//Arguments reach the tool as one re-parsed string, so paths holding a space need quoting
		internal static string Quote(string path)
		{
			if (string.IsNullOrEmpty(path) || !path.Contains(' ') || path.Contains('"'))
				return path;

			return "\"" + path + "\"";
		}
	}
}
