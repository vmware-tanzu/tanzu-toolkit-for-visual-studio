using System.Collections.Generic;
using System.Security;
using System.Threading.Tasks;
using TanzuForVS.CloudFoundryApiClient.Models.OrgsResponse;

namespace TanzuForVS.Services.CloudFoundry
{
    public interface ICloudFoundryService
    {
        bool IsLoggedIn { get; }
        string LoginFailureMessage { get; }
        Task<ConnectResult> ConnectToCFAsync(string target, string username, SecureString password, string httpProxy, bool skipSsl);
        Task<List<string>> GetOrgNamesAsync(string target, string acessToken);
    }
}
