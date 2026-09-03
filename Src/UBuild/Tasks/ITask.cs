namespace UBuild.Tasks
{
	internal interface ITask
	{
		void Display(Output output);
		bool Run(Output output);
	}
}
