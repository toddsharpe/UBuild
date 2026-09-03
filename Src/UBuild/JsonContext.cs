using System.Text.Json.Serialization;
using UBuild.Models;
using Environment = UBuild.Models.Environment;

namespace UBuild.Config
{
	//Compile-time serializer metadata so publish can trim/AOT without cutting the models.
	[JsonSerializable(typeof(Environment))]
	[JsonSerializable(typeof(Executable))]
	[JsonSerializable(typeof(List<CompileCommand>))]
	internal partial class UBuildJsonContext : JsonSerializerContext
	{
	}
}
