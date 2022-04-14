using Serilog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using Tanzu.Toolkit.Services.Logging;

namespace Tanzu.Toolkit.Services.CommandProcess
{
    public class CommandProcessService : ICommandProcessService
    {
        private Process _process;
        private Action<string> _stdOutCallback;
        private Action<string> _stdErrCallback;
        private string _stdOutAggregator = "";
        private string _stdErrAggregator = "";
        private List<string> _cancelTriggers;
        private int _customExitCode = 1;
        private readonly ILogger _logger;

        public CommandProcessService(ILoggingService loggingService)
        {
            _logger = loggingService.Logger;
        }

        public CommandResult RunExecutable(string executableFilePath, string arguments, string workingDir, Dictionary<string, string> envVars = null, Action<string> stdOutAction = null, Action<string> stdErrAction = null, List<string> processCancelTriggers = null)
        {
            // Create Process
            _process = new Process();
            _process.StartInfo.FileName = executableFilePath;
            _process.StartInfo.Arguments = arguments;
            _process.StartInfo.UseShellExecute = false;
            _process.StartInfo.RedirectStandardOutput = true;
            _process.StartInfo.RedirectStandardError = true;
            _process.StartInfo.CreateNoWindow = true;

            if (workingDir != null)
            {
                _process.StartInfo.WorkingDirectory = workingDir;
            }

            // Set environment variables
            if (envVars != null)
            {
                foreach (var envVar in envVars)
                {
                    _process.StartInfo.EnvironmentVariables[envVar.Key] = envVar.Value;
                }
            }

            // Set output and error handlers
            _cancelTriggers = processCancelTriggers ?? new List<string>();
            _stdOutCallback = stdOutAction;
            _stdErrCallback = stdErrAction;
            _stdOutAggregator = "";
            _stdErrAggregator = "";
            _process.OutputDataReceived += new DataReceivedEventHandler(OutputRecorder);
            _process.ErrorDataReceived += new DataReceivedEventHandler(ErrorRecorder);

            // Start process and handlers
            _process.Start();
            _process.BeginOutputReadLine();
            _process.BeginErrorReadLine();

            // Begin blocking call
            _process.WaitForExit();

            int exitCode;

            try
            {
                exitCode = _process.ExitCode; // throws InvalidOperationException if process has already been closed
            }
            catch (InvalidOperationException)
            {
                exitCode = _customExitCode;
            }

            return new CommandResult(_stdOutAggregator, _stdErrAggregator, exitCode);
        }

        public Process StartProcess(string executableFilePath, string arguments, string workingDir, Dictionary<string, string> envVars = null, Action<string> stdOutAction = null, Action<string> stdErrAction = null, List<string> processCancelTriggers = null)
        {
            try
            {
                // Create Process
                var process = new Process();
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

                return process;
            }
            catch (Exception ex)
            {
                _logger.Error("CommandProcessService.StartProcess encountered an exception: {StartProcessException}", ex);
                return null;
            }
        }

        private void OutputRecorder(object sendingProcess, DataReceivedEventArgs outLine)
        {
            var outContent = outLine.Data;
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
            var errContent = errLine.Data;
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
