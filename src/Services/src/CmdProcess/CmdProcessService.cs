﻿using System.Diagnostics;
using System.Threading.Tasks;
using Tanzu.Toolkit.Services.OutputHandler;
using static Tanzu.Toolkit.Services.OutputHandler.OutputHandler;

namespace Tanzu.Toolkit.Services.CmdProcess
{
    public class CmdProcessService : ICmdProcessService
    {
        private StdOutDelegate _stdOutCallback;
        private StdErrDelegate _stdErrCallback;
        private string _stdOutAggregator = "";
        private string _stdErrAggregator = "";

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
            // Create Process
            Process process = new Process();
            process.StartInfo.FileName = "cmd.exe";
            process.StartInfo.Arguments = "/c " + arguments;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.CreateNoWindow = true;
            if (workingDir != null)
            {
                process.StartInfo.WorkingDirectory = workingDir;
            }

            // Set output and error handlers
            _stdOutCallback = stdOutDelegate;
            _stdErrCallback = stdErrDelegate;
            _stdOutAggregator = "";
            _stdErrAggregator = "";
            process.OutputDataReceived += new DataReceivedEventHandler(OutputRecorder);
            process.ErrorDataReceived += new DataReceivedEventHandler(ErrorRecorder);

            // Start process and handlers
            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            // Begin blocking call
            await Task.Run(() => process.WaitForExit());

            return new CmdResult(_stdOutAggregator, _stdErrAggregator, process.ExitCode);
        }

        public CmdResult RunCommand(string executableFilePath, string arguments, string workingDir, StdOutDelegate stdOutDelegate, StdErrDelegate stdErrDelegate)
        {
            // Create Process
            Process process = new Process();
            process.StartInfo.FileName = executableFilePath;
            process.StartInfo.Arguments = arguments;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.CreateNoWindow = true;
            if (workingDir != null)
            {
                process.StartInfo.WorkingDirectory = workingDir;
            }

            // Set output and error handlers
            _stdOutCallback = stdOutDelegate;
            _stdErrCallback = stdErrDelegate;
            _stdOutAggregator = "";
            _stdErrAggregator = "";
            process.OutputDataReceived += new DataReceivedEventHandler(OutputRecorder);
            process.ErrorDataReceived += new DataReceivedEventHandler(ErrorRecorder);

            // Start process and handlers
            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            // Begin blocking call
            process.WaitForExit();

            return new CmdResult(_stdOutAggregator, _stdErrAggregator, process.ExitCode);
        }

        public CmdResult ExecuteWindowlessCommand(string arguments, string workingDir)
        {
            // Create Process
            Process process = new Process();
            process.StartInfo.FileName = "cmd.exe";
            process.StartInfo.Arguments = "/c " + arguments;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.CreateNoWindow = true;
            process.StartInfo.WorkingDirectory = workingDir;

            // Set output and error handlers
            _stdOutAggregator = "";
            _stdErrAggregator = "";
            process.OutputDataReceived += new DataReceivedEventHandler(OutputRecorder);
            process.ErrorDataReceived += new DataReceivedEventHandler(ErrorRecorder);

            // Start process and handlers
            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            process.WaitForExit();

            return new CmdResult(_stdOutAggregator, _stdErrAggregator, process.ExitCode);
        }

        private void OutputRecorder(object sendingProcess, DataReceivedEventArgs outLine)
        {
            string outContent = outLine.Data;
            if (outContent != null)
            {
                _stdOutAggregator += $"{outContent}\n";
                _stdOutCallback?.Invoke(outContent);
            }
        }

        private void ErrorRecorder(object sendingProcess, DataReceivedEventArgs errLine)
        {
            string errContent = errLine.Data;
            if (errContent != null)
            {
                _stdErrAggregator += $"{errContent}\n";
                _stdErrCallback?.Invoke(errContent);
            }
        }
    }
}
