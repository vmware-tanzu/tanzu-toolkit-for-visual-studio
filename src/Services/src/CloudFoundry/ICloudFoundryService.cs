using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Security;
using System.Threading.Tasks;
using Tanzu.Toolkit.Models;

namespace Tanzu.Toolkit.Services.CloudFoundry
{
    public interface ICloudFoundryService
    {
        Task<DetailedResult> LoginWithCredentials(string target, string username, SecureString password, bool skipSsl);
        Task<DetailedResult<List<CloudFoundryOrganization>>> GetOrgsForCfInstanceAsync(CloudFoundryInstance cf, bool skipSsl = true, int retryAmount = 1);
        Task<DetailedResult<List<CloudFoundrySpace>>> GetSpacesForOrgAsync(CloudFoundryOrganization org, bool skipSsl = true, int retryAmount = 1);
        Task<DetailedResult<List<CloudFoundryApp>>> GetAppsForSpaceAsync(CloudFoundrySpace space, bool skipSsl = true, int retryAmount = 1);
        Task<DetailedResult> StopAppAsync(CloudFoundryApp app, bool skipSsl = true, int retryAmount = 1);
        Task<DetailedResult> StartAppAsync(CloudFoundryApp app, bool skipSsl = true, int retryAmount = 1);
        Task<DetailedResult> DeleteAppAsync(CloudFoundryApp app, bool skipSsl = true, bool removeRoutes = false, int retryAmount = 1);
        Task<DetailedResult<string>> GetRecentLogsAsync(CloudFoundryApp app);
        Task<DetailedResult<List<CfBuildpack>>> GetBuildpacksAsync(string apiAddress, int retryAmount = 1);
        DetailedResult CreateManifestFile(string location, AppManifest manifest);
        Task<DetailedResult> DeployAppAsync(AppManifest appManifest, string defaultAppPath, CloudFoundryInstance targetCf, CloudFoundryOrganization targetOrg, CloudFoundrySpace targetSpace, Action<string> stdOutCallback, Action<string> stdErrCallback);
        Task<DetailedResult<List<string>>> GetStackNamesAsync(CloudFoundryInstance cf, int retryAmount = 1);
        Task<DetailedResult<string>> GetSsoPrompt(string cfApiAddress, bool skipSsl = false);
        Task<DetailedResult> LoginWithSsoPasscode(string cfApiAddress, string passcode);
        DetailedResult TargetApi(string targetApiAddress, bool skipSsl);
        bool IsValidConnection();
        void LogoutCfUser();
        Task<DetailedResult> DeleteAllRoutesForAppAsync(CloudFoundryApp app);
        Task<DetailedResult<List<CloudFoundryRoute>>> GetRoutesForAppAsync(CloudFoundryApp app, int retryAmount = 1);
        DetailedResult<Process> StreamAppLogs(CloudFoundryApp app, Action<string> stdOutCallback, Action<string> stdErrCallback);
    }
}