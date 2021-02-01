using System.Threading.Tasks;
using static TanzuForVS.Services.CfCli.StdOutHandler;

namespace TanzuForVS.Services.CmdProcess
{
    public interface ICmdProcessService
    {
        Task<bool> ExecuteWindowlessCommandAsync(string arguments, string workingDir, StdOutDelegate stdOutHandler);
        void InitiateWindowlessCommand(string arguments, string workingDir);
    }
}