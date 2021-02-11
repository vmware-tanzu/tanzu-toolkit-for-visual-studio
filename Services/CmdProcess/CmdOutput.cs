namespace Tanzu.Toolkit.VisualStudio.Services.CmdProcess
{
    public class CmdOutput
    {
        public string StdOut { get; }
        public string StdErr { get; }

        public int ExitCode { get; }

        public CmdOutput(string stdOut, string stdErr, int exitCode)
        {
            StdOut = stdOut;
            StdErr = stdErr;
            ExitCode = exitCode;
        }
    }
}
