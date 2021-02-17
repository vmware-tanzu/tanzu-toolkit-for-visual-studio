using System.Threading.Tasks;
using static Tanzu.Toolkit.VisualStudio.Services.OutputHandler.OutputHandler;

namespace Tanzu.Toolkit.VisualStudio.Services.CmdProcess
{
    public interface ICmdProcessService
    {
        CmdResult ExecuteWindowlessCommand(string arguments, string workingDir);
        Task<CmdResult> InvokeWindowlessCommandAsync(string arguments, string workingDir, StdOutDelegate stdOutDelegate, StdErrDelegate stdErrDelegate);
    }
}