using System.Collections.Generic;
using System.Security;
using System.Threading.Tasks;
using Tanzu.Toolkit.VisualStudio.Models;
using static Tanzu.Toolkit.VisualStudio.Services.OutputHandler.OutputHandler;

namespace Tanzu.Toolkit.VisualStudio.Services.CloudFoundry
{
    public interface ICloudFoundryService
    {
        CloudFoundryInstance ActiveCloud { get; set; }
        Dictionary<string, CloudFoundryInstance> CloudFoundryInstances { get; }
        string LoginFailureMessage { get; }

        void AddCloudFoundryInstance(string name, string apiAddress, string accessToken);
        Task<ConnectResult> ConnectToCFAsync(string target, string username, SecureString password, string httpProxy, bool skipSsl);
        Task<DetailedResult<List<CloudFoundryOrganization>>> GetOrgsForCfInstanceAsync(CloudFoundryInstance cf, bool skipSsl = true);
        Task<DetailedResult<List<CloudFoundrySpace>>> GetSpacesForOrgAsync(CloudFoundryOrganization org, bool skipSsl = true);
        Task<DetailedResult<List<CloudFoundryApp>>> GetAppsForSpaceAsync(CloudFoundrySpace space, bool skipSsl = true);
        Task<DetailedResult> StopAppAsync(CloudFoundryApp app, bool skipSsl = true);
        Task<DetailedResult> StartAppAsync(CloudFoundryApp app, bool skipSsl = true);
        Task<DetailedResult> DeleteAppAsync(CloudFoundryApp app, bool skipSsl = true, bool removeRoutes = true);
        Task<DetailedResult> DeployAppAsync(CloudFoundryInstance targetCf, CloudFoundryOrganization targetOrg, CloudFoundrySpace targetSpace, string appName, string appProjPath, bool fullFrameworkDeployment, StdOutDelegate stdOutCallback, StdErrDelegate stdErrCallback);
        void RemoveCloudFoundryInstance(string name);
    }
}