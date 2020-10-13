using System.Collections.Generic;
using System.Security;
using System.Threading.Tasks;
using TanzuForVS.Services.Models;

namespace TanzuForVS.Services.CloudFoundry
{
    public interface ICloudFoundryService
    {
        CloudFoundryInstance ActiveCloud { get; set; }
        Dictionary<string, CloudFoundryInstance> CloudFoundryInstances { get; }
        string LoginFailureMessage { get; }
        string InstanceName { get; set; }
        void AddCloudFoundryInstance(string name);
        Task<ConnectResult> ConnectToCFAsync(string target, string username, SecureString password, string httpProxy, bool skipSsl);
        Task<List<string>> GetOrgNamesAsync(string target, string acessToken);
    }
}
