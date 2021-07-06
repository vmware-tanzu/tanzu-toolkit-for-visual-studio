using System.Collections.Generic;
using System.Diagnostics;
using static Tanzu.Toolkit.Services.OutputHandler.OutputHandler;

namespace Tanzu.Toolkit.Services.CmdProcess
{
    public class CmdProcessService : ICmdProcessService
    {
        private StdOutDelegate _stdOutCallback;
        private StdErrDelegate _stdErrCallback;
        private string _stdOutAggregator = "";
        private string _stdErrAggregator = "";

        public CmdResult RunCommand(string executableFilePath, string arguments, string workingDir, Dictionary<string, string> envVars = null, StdOutDelegate stdOutDelegate = null, StdErrDelegate stdErrDelegate = null)
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
                foreach (string key in envVars.Keys)
                {
                    process.StartInfo.EnvironmentVariables[key] = envVars[key];
                }
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
