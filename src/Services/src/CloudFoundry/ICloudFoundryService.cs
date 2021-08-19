using System.Collections.Generic;
using System.Security;
using System.Threading.Tasks;
using Tanzu.Toolkit.Models;
using Tanzu.Toolkit.Services.CloudFoundry;
using static Tanzu.Toolkit.Services.OutputHandler.OutputHandler;

namespace Tanzu.Toolkit.Services.CloudFoundry
{
    public interface ICloudFoundryService
    {
        CloudFoundryInstance ActiveCloud { get; set; }
        Dictionary<string, CloudFoundryInstance> CloudFoundryInstances { get; }
        string LoginFailureMessage { get; }

        void AddCloudFoundryInstance(string name, string apiAddress);
        Task<ConnectResult> ConnectToCFAsync(string target, string username, SecureString password, string httpProxy, bool skipSsl);
        Task<DetailedResult<List<CloudFoundryOrganization>>> GetOrgsForCfInstanceAsync(CloudFoundryInstance cf, bool skipSsl = true, int retryAmount = 1);
        Task<DetailedResult<List<CloudFoundrySpace>>> GetSpacesForOrgAsync(CloudFoundryOrganization org, bool skipSsl = true, int retryAmount = 1);
        Task<DetailedResult<List<CloudFoundryApp>>> GetAppsForSpaceAsync(CloudFoundrySpace space, bool skipSsl = true, int retryAmount = 1);
        Task<DetailedResult> StopAppAsync(CloudFoundryApp app, bool skipSsl = true, int retryAmount = 1);
        Task<DetailedResult> StartAppAsync(CloudFoundryApp app, bool skipSsl = true, int retryAmount = 1);
        Task<DetailedResult> DeleteAppAsync(CloudFoundryApp app, bool skipSsl = true, bool removeRoutes = true, int retryAmount = 1);
        Task<DetailedResult> DeployAppAsync(CloudFoundryInstance targetCf, CloudFoundryOrganization targetOrg, CloudFoundrySpace targetSpace, string appName, string appProjPath, bool fullFrameworkDeployment, StdOutDelegate stdOutCallback, StdErrDelegate stdErrCallback, string manifestPath = null);
        void RemoveCloudFoundryInstance(string name);
        Task<DetailedResult<string>> GetRecentLogs(CloudFoundryApp app);
    }
}