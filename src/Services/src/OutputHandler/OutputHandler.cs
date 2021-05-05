namespace Tanzu.Toolkit.Services.OutputHandler
{
    public class OutputHandler : IOutputHandler
    {
        public delegate void StdOutDelegate(string stdOutAccumulator);
        public delegate void StdErrDelegate(string stdErrAccumulator);
    }
}
