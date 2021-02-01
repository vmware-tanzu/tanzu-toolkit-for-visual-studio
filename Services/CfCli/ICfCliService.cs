using System.Threading.Tasks;
using static TanzuForVS.Services.OutputHandler;

namespace TanzuForVS.Services.CfCli
{
    public interface ICfCliService
    {
        Task<DetailedResult> ExecuteCfCliCommandAsync(string arguments, StdOutDelegate stdOutHandler = null, string workingDir = null);
        void InitiateCfCliCommand(string arguments, string workingDir);
    }
}