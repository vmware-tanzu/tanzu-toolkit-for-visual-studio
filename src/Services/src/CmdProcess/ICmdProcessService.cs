using System.Collections.Generic;
using static Tanzu.Toolkit.Services.OutputHandler.OutputHandler;

namespace Tanzu.Toolkit.Services.CmdProcess
{
    public interface ICmdProcessService
    {
        CmdResult RunExecutable(string executableFilePath, string arguments, string workingDir, Dictionary<string, string> envVars = null, StdOutDelegate stdOutDelegate = null, StdErrDelegate stdErrDelegate = null);
    }
}