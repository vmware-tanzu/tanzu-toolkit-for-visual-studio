namespace Tanzu.Toolkit.Services.CommandProcess
{
    public class CommandResult
    {
        public string StdOut { get; }
        public string StdErr { get; }

        public int ExitCode { get; }

        public CommandResult(string stdOut, string stdErr, int exitCode)
        {
            StdOut = stdOut;
            StdErr = stdErr;
            ExitCode = exitCode;
        }
    }
}