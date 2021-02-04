using System.Threading.Tasks;
using static Tanzu.Toolkit.VisualStudio.Services.OutputHandler.OutputHandler;

namespace Tanzu.Toolkit.VisualStudio.Services.CfCli
{
    public interface ICfCliService
    {
        Task<DetailedResult> ExecuteCfCliCommandAsync(string arguments, StdOutDelegate stdOutHandler = null, string workingDir = null);
        void InitiateCfCliCommand(string arguments, string workingDir);
    }
}