using System.Diagnostics;
using System.Threading.Tasks;
using static Tanzu.Toolkit.VisualStudio.Services.OutputHandler.OutputHandler;

namespace Tanzu.Toolkit.VisualStudio.Services.CmdProcess
{
    public class CmdProcessService : ICmdProcessService
    {
        StdOutDelegate StdOutHandler;
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
        public async Task<bool> InvokeWindowlessCommandAsync(string arguments, string workingDir, StdOutDelegate stdOutHandler)
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
            StdOutHandler = stdOutHandler;
            process.OutputDataReceived += new DataReceivedEventHandler(OutputHandler);
            process.ErrorDataReceived += new DataReceivedEventHandler(ErrorHandler);

            //* Start process and handlers
            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            // Begin blocking call
            await Task.Run(() => process.WaitForExit());

            if (process.ExitCode == 0) return true;
            return false;
        }

        public CmdOutput ExecuteWindowlessCommand(string arguments, string workingDir)
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
            process.OutputDataReceived += new DataReceivedEventHandler(StdOutRecorder);
            process.ErrorDataReceived += new DataReceivedEventHandler(StdErrRecorder);

            //* Start process and handlers
            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            process.WaitForExit();

            return new CmdOutput(StdOutAggregator, StdErrAggregator, process.ExitCode);
        }

        private void OutputHandler(object sendingProcess, DataReceivedEventArgs outLine)
        {
            string outContent = outLine.Data;
            if (outContent != null)
            {
                StdOutHandler?.Invoke(outContent);
            }
        }

        private void ErrorHandler(object sendingProcess, DataReceivedEventArgs errLine)
        {
            string errContent = errLine.Data;
            if (errContent != null)
            {
                StdOutHandler?.Invoke(errContent);
            }
        }
        
        private void StdOutRecorder(object sendingProcess, DataReceivedEventArgs outLine)
        {
            string outContent = outLine.Data;
            if (outContent != null)
            {
                StdOutAggregator += $"{outContent}\n";
            }
        }

        private void StdErrRecorder(object sendingProcess, DataReceivedEventArgs errLine)
        {
            string errContent = errLine.Data;
            if (errContent != null)
            {
                StdErrAggregator += $"{errContent}\n";
            }
        }

    }
}
