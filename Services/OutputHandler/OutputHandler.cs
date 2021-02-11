namespace Tanzu.Toolkit.VisualStudio.Services.OutputHandler
{
    public class OutputHandler : IOutputHandler
    {
        public delegate void StdOutDelegate(string stdOutAccumulator);
    }
}
