using System;
using System.Collections.Generic;
using System.Diagnostics;
using static Tanzu.Toolkit.Services.OutputHandler.OutputHandler;

namespace Tanzu.Toolkit.Services.CommandProcess
{
    public class CommandProcessService : ICommandProcessService
    {
        private StdOutDelegate _stdOutCallback;
        private StdErrDelegate _stdErrCallback;
        private string _stdOutAggregator = "";
        private string _stdErrAggregator = "";
        private List<string> _cancelTriggers;
        private int _customExitCode = 1;

        public CommandResult RunExecutable(string executableFilePath, string arguments, string workingDir, Dictionary<string, string> envVars = null, StdOutDelegate stdOutAction = null, StdErrDelegate stdErrAction = null, List<string> processCancelTriggers = null)
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

            // Set environment variables
            if (envVars != null)
            {
                foreach (var envVar in envVars)
                {
                    process.StartInfo.EnvironmentVariables[envVar.Key] = envVar.Value;
                }
            }

            // Set output and error handlers
            _cancelTriggers = processCancelTriggers ?? new List<string>();
            _stdOutCallback = stdOutAction;
            _stdErrCallback = stdErrAction;
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

            int exitCode;

            try
            {
                exitCode = process.ExitCode; // throws InvalidOperationException if process has already been closed
            }
            catch (InvalidOperationException)
            {
                exitCode = _customExitCode;
            }

            return new CommandResult(_stdOutAggregator, _stdErrAggregator, exitCode);
        }

        private void OutputRecorder(object sendingProcess, DataReceivedEventArgs outLine)
        {
            string outContent = outLine.Data;
            if (outContent != null)
            {
                _stdOutAggregator += $"{outContent}\n";
                _stdOutCallback?.Invoke(outContent);

                if (_cancelTriggers.Contains(outContent))
                {
                    _customExitCode = 0;
                    (sendingProcess as Process)?.Close();
                }
            }
        }

        private void ErrorRecorder(object sendingProcess, DataReceivedEventArgs errLine)
        {
            string errContent = errLine.Data;
            if (errContent != null)
            {
                _stdErrAggregator += $"{errContent}\n";
                _stdErrCallback?.Invoke(errContent);

                if (_cancelTriggers.Contains(errContent))
                {
                    _customExitCode = 1;
                    (sendingProcess as Process)?.Close();
                }
            }
        }
    }
}
