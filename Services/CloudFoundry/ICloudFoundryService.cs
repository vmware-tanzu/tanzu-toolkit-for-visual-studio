using System.Collections.Generic;
using System.Security;
using System.Threading.Tasks;
using TanzuForVS.Models;

namespace TanzuForVS.Services.CloudFoundry
{
    public interface ICloudFoundryService
    {
        CloudFoundryInstance ActiveCloud { get; set; }
        Dictionary<string, CloudFoundryInstance> CloudFoundryInstances { get; }
        string LoginFailureMessage { get; }
        void AddCloudFoundryInstance(string name, string apiAddress, string accessToken);
        Task<ConnectResult> ConnectToCFAsync(string target, string username, SecureString password, string httpProxy, bool skipSsl);
        Task<List<CloudFoundryOrganization>> GetOrgsForCfInstanceAsync(CloudFoundryInstance cf);
        Task<List<CloudFoundrySpace>> GetSpacesForOrgAsync(CloudFoundryOrganization org);
        Task<List<CloudFoundryApp>> GetAppsForSpaceAsync(CloudFoundrySpace space);
        Task<bool> StopAppAsync(CloudFoundryApp app);
    }
}
