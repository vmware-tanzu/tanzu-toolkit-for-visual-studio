using System.Threading.Tasks;

namespace TanzuForVS.Services.CmdProcess
{
    public interface ICmdProcessService
    {
        Task<bool> ExecuteWindowlessCommandAsync(string arguments, string workingDir);
        void InitiateWindowlessCommand(string arguments, string workingDir);
    }
}