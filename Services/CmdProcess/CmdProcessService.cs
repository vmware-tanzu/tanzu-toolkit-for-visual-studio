using System.Diagnostics;
using System.Threading.Tasks;

namespace TanzuForVS.Services.CmdProcess
{
    public class CmdProcessService : ICmdProcessService
    {
        public CmdProcessService()
        {

        }

        /// <summary>
        /// Executes a new command with the given arguments. Returns true if the command process exits with a 0 exit code, otherwise returns false.
        /// </summary>
        /// <param name="arguments"></param>
        /// <param name="workingDir"></param>
        /// <returns></returns>
        public async Task<bool> ExecuteWindowlessCommandAsync(string arguments, string workingDir)
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

        public void InitiateWindowlessCommand(string arguments, string workingDir)
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
            process.OutputDataReceived += new DataReceivedEventHandler(OutputHandler);
            process.ErrorDataReceived += new DataReceivedEventHandler(ErrorHandler);

            //* Start process and handlers
            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
        }

        private void OutputHandler(object sendingProcess, DataReceivedEventArgs outLine)
        {
            if (outLine.Data != null)
            {
            }
        }

        private void ErrorHandler(object sendingProcess, DataReceivedEventArgs outLine)
        {
            if (outLine.Data != null)
            {
            }
        }
    }
}
