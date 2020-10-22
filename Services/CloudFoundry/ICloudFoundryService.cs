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
        Task<List<CloudFoundryOrganization>> GetOrgsAsync(string target, string accessToken);
        Task<List<CloudFoundrySpace>> GetSpacesAsync(string target, string accessToken, string orgId);
        Task<List<CloudFoundryApp>> GetAppsAsync(string target, string accessToken, string spaceId);
    }
}
