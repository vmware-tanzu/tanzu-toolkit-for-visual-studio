using static Tanzu.Toolkit.Services.OutputHandler.OutputHandler;

namespace Tanzu.Toolkit.Services.CmdProcess
{
    public interface ICmdProcessService
    {
        CmdResult RunCommand(string executableFilePath, string arguments, string workingDir, StdOutDelegate stdOutDelegate, StdErrDelegate stdErrDelegate);
    }
}