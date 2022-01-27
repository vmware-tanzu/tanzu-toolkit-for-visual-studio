using System;
using System.Diagnostics;
using System.Security;
using System.Threading.Tasks;

namespace Tanzu.Toolkit.Services.CfCli
{
    public interface ICfCliService
    {
        string GetOAuthToken();
        DetailedResult TargetApi(string apiAddress, bool skipSsl);
        Task<DetailedResult> AuthenticateAsync(string username, SecureString password);
        DetailedResult TargetOrg(string orgName);
        DetailedResult TargetSpace(string spaceName);
        Task<DetailedResult> StopAppByNameAsync(string appName);
        Task<DetailedResult> StartAppByNameAsync(string appName);
        Task<DetailedResult> DeleteAppByNameAsync(string appName, bool removeMappedRoutes = true);
        Task<Version> GetApiVersion();
        Task<DetailedResult<string>> GetRecentAppLogs(string appName, string orgName, string spaceName);
        void ClearCachedAccessToken();
        Task<DetailedResult> PushAppAsync(string manifestPath, string appDirPath, string orgName, string spaceName, Action<string> stdOutCallback, Action<string> stdErrCallback);
        Task<DetailedResult> LoginWithSsoPasscode(string apiAddress, string passcode);
        DetailedResult Logout();
        DetailedResult<Process> StreamAppLogs(string appName, string orgName, string spaceName, Action<string> stdOutCallback, Action<string> stdErrCallback);
    }
}