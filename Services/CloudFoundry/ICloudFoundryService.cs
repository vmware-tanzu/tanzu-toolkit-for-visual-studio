using System.Security;
using System.Threading.Tasks;

namespace TanzuForVS.Services.CloudFoundry
{
    public interface ICloudFoundryService
    {
        bool IsLoggedIn { get; }
        string LoginFailureMessage { get; }
        Task<ConnectResult> ConnectToCFAsync(string target, string username, SecureString password, string httpProxy, bool skipSsl);
    }
}
