namespace Tanzu.Toolkit.Services.CmdProcess
{
    public class CmdResult
    {
        public string StdOut { get; }
        public string StdErr { get; }

        public int ExitCode { get; }

        public CmdResult(string stdOut, string stdErr, int exitCode)
        {
            StdOut = stdOut;
            StdErr = stdErr;
            ExitCode = exitCode;
        }
    }
}
