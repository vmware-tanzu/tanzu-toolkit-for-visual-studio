using System.Diagnostics;
using System.Threading.Tasks;
using static Tanzu.Toolkit.VisualStudio.Services.OutputHandler.OutputHandler;

namespace Tanzu.Toolkit.VisualStudio.Services.CmdProcess
{
    public class CmdProcessService : ICmdProcessService
    {
        StdOutDelegate StdOutCallback;
        StdErrDelegate StdErrCallback;
        private string StdOutAggregator = "";
        private string StdErrAggregator = "";

        public CmdProcessService()
        {
        }

        /// <summary>
        /// Begins a new command with the given arguments. Returns true if the command process exits with a 0 exit code, otherwise returns false.
        /// </summary>
        /// <param name="arguments"></param>
        /// <param name="workingDir"></param>
        /// <returns></returns>
        public async Task<CmdResult> InvokeWindowlessCommandAsync(string arguments, string workingDir, StdOutDelegate stdOutDelegate, StdErrDelegate stdErrDelegate)
        {
            //* Create your Process
            Process process = new Process();
            process.StartInfo.FileName = "cmd.exe";
            process.StartInfo.Arguments = "/c " + arguments;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.CreateNoWindow = true;
            if (workingDir != null) process.StartInfo.WorkingDirectory = workingDir;

            //* Set your output and error (asynchronous) handlers
            StdOutCallback = stdOutDelegate;
            StdErrCallback = stdErrDelegate;
            StdOutAggregator = "";
            StdErrAggregator = "";
            process.OutputDataReceived += new DataReceivedEventHandler(OutputRecorder);
            process.ErrorDataReceived += new DataReceivedEventHandler(ErrorRecorder);

            //* Start process and handlers
            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            // Begin blocking call
            await Task.Run(() => process.WaitForExit());

            return new CmdResult(StdOutAggregator, StdErrAggregator, process.ExitCode);
        }

        public CmdResult ExecuteWindowlessCommand(string arguments, string workingDir)
        {
            //* Create your Process
            Process process = new Process();
            process.StartInfo.FileName = "cmd.exe";
            process.StartInfo.Arguments = "/c " + arguments;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.CreateNoWindow = true;
            process.StartInfo.WorkingDirectory = workingDir;

            //* Set your output and error (asynchronous) handlers
            StdOutAggregator = "";
            StdErrAggregator = "";
            process.OutputDataReceived += new DataReceivedEventHandler(OutputRecorder);
            process.ErrorDataReceived += new DataReceivedEventHandler(ErrorRecorder);

            //* Start process and handlers
            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            process.WaitForExit();

            return new CmdResult(StdOutAggregator, StdErrAggregator, process.ExitCode);
        }
        
        private void OutputRecorder(object sendingProcess, DataReceivedEventArgs outLine)
        {
            string outContent = outLine.Data;
            if (outContent != null)
            {
                StdOutAggregator += $"{outContent}\n";
                StdOutCallback?.Invoke(outContent);
            }
        }

        private void ErrorRecorder(object sendingProcess, DataReceivedEventArgs errLine)
        {
            string errContent = errLine.Data;
            if (errContent != null)
            {
                StdErrAggregator += $"{errContent}\n";
                StdErrCallback?.Invoke(errContent);
            }
        }

    }
}
