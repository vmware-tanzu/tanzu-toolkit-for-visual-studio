using System.Threading.Tasks;

namespace TanzuForVS.Services.CfCli
{
    public interface ICfCliService
    {
        Task<DetailedResult> ExecuteCfCliCommandAsync(string arguments, string workingDir = null);
        void InitiateCfCliCommand(string arguments, string workingDir);
    }
}