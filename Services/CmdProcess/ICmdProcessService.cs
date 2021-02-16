using System.Threading.Tasks;
using static Tanzu.Toolkit.VisualStudio.Services.OutputHandler.OutputHandler;

namespace Tanzu.Toolkit.VisualStudio.Services.CmdProcess
{
    public interface ICmdProcessService
    {
        Task<bool> InvokeWindowlessCommandAsync(string arguments, string workingDir, StdOutDelegate stdOutHandler);
        CmdResult ExecuteWindowlessCommand(string arguments, string workingDir);
    }
}