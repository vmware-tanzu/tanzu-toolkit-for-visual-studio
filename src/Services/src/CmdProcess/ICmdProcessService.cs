using System.Threading.Tasks;
using static Tanzu.Toolkit.Services.OutputHandler.OutputHandler;

namespace Tanzu.Toolkit.Services.CmdProcess
{
    public interface ICmdProcessService
    {
        CmdResult ExecuteWindowlessCommand(string arguments, string workingDir);
        CmdResult RunCommand(string executableFilePath, string arguments, string workingDir, StdOutDelegate stdOutDelegate, StdErrDelegate stdErrDelegate);
    }
}