using System.Security;
using System.Threading.Tasks;
using Tanzu.Toolkit.VisualStudio.Services.CmdProcess;
using static Tanzu.Toolkit.VisualStudio.Services.OutputHandler.OutputHandler;

namespace Tanzu.Toolkit.VisualStudio.Services.CfCli
{
    public interface ICfCliService
    {
        CmdResult ExecuteCfCliCommand(string arguments, string workingDir = null);
        Task<DetailedResult> InvokeCfCliAsync(string arguments, StdOutDelegate stdOutCallback = null, StdErrDelegate stdErrCallback = null, string workingDir = null);
        
        bool Authenticate(string username, SecureString password);
        string GetOAuthToken();
        bool TargetApi(string apiAddress, bool skipSsl);
    }
}