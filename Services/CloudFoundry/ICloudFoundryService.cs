using System.Collections.Generic;
using System.Security;
using System.Threading.Tasks;
using TanzuForVS.CloudFoundryApiClient.Models.OrgsResponse;
using TanzuForVS.Services.Models;

namespace TanzuForVS.Services.CloudFoundry
{
    public interface ICloudFoundryService
    {
        CloudItem ActiveCloud { get; set; }
        Dictionary<string, CloudItem> CloudItems { get; }
        string LoginFailureMessage { get; }
        string InstanceName { get; set; }
        void AddCloudItem(string name);
        Task<ConnectResult> ConnectToCFAsync(string target, string username, SecureString password, string httpProxy, bool skipSsl);
        Task<List<string>> GetOrgNamesAsync(string target, string acessToken);
    }
}
