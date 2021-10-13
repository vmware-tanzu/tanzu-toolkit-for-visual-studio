using System.Collections.Generic;
using System.Security;
using System.Threading.Tasks;
using Tanzu.Toolkit.Models;
using static Tanzu.Toolkit.Services.OutputHandler.OutputHandler;

namespace Tanzu.Toolkit.Services.CloudFoundry
{
    public interface ICloudFoundryService
    {
        Task<ConnectResult> ConnectToCFAsync(string target, string username, SecureString password, bool skipSsl);
        Task<DetailedResult<List<CloudFoundryOrganization>>> GetOrgsForCfInstanceAsync(CloudFoundryInstance cf, bool skipSsl = true, int retryAmount = 1);
        Task<DetailedResult<List<CloudFoundrySpace>>> GetSpacesForOrgAsync(CloudFoundryOrganization org, bool skipSsl = true, int retryAmount = 1);
        Task<DetailedResult<List<CloudFoundryApp>>> GetAppsForSpaceAsync(CloudFoundrySpace space, bool skipSsl = true, int retryAmount = 1);
        Task<DetailedResult> StopAppAsync(CloudFoundryApp app, bool skipSsl = true, int retryAmount = 1);
        Task<DetailedResult> StartAppAsync(CloudFoundryApp app, bool skipSsl = true, int retryAmount = 1);
        Task<DetailedResult> DeleteAppAsync(CloudFoundryApp app, bool skipSsl = true, bool removeRoutes = true, int retryAmount = 1);
        Task<DetailedResult> DeployAppAsync(CloudFoundryInstance targetCf, CloudFoundryOrganization targetOrg, CloudFoundrySpace targetSpace, string appName, string pathToDeploymentDirectory, bool fullFrameworkDeployment, StdOutDelegate stdOutCallback, StdErrDelegate stdErrCallback, string stack, bool binaryDeployment, string projectName, string manifestPath = null, string buildpack = null);
        Task<DetailedResult<string>> GetRecentLogs(CloudFoundryApp app);
    }
}