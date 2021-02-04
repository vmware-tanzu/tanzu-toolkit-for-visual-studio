using System.Threading.Tasks;
using static Tanzu.Toolkit.VisualStudio.Services.OutputHandler.OutputHandler;

namespace Tanzu.Toolkit.VisualStudio.Services.CmdProcess
{
    public interface ICmdProcessService
    {
        Task<bool> ExecuteWindowlessCommandAsync(string arguments, string workingDir, StdOutDelegate stdOutHandler);
        void InitiateWindowlessCommand(string arguments, string workingDir);
    }
}