using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Tanzu.Toolkit.Services.CommandProcess
{
    public interface ICommandProcessService
    {
        CommandResult RunExecutable(string executableFilePath, string arguments, string workingDir, Dictionary<string, string> envVars = null, Action<string> stdOutDelegate = null,
            Action<string> stdErrDelegate = null, List<string> processCancelTriggers = null);

        Process StartProcess(string executableFilePath, string arguments, string workingDir, Dictionary<string, string> envVars = null, Action<string> stdOutDelegate = null,
            Action<string> stdErrDelegate = null, List<string> processCancelTriggers = null);
    }
}