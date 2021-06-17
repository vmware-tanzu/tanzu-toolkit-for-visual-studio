﻿using System;
using System.Collections.Generic;
using System.Security;
using System.Threading.Tasks;
using Tanzu.Toolkit.Services.CfCli.Models.Apps;
using Tanzu.Toolkit.Services.CfCli.Models.Orgs;
using Tanzu.Toolkit.Services.CfCli.Models.Spaces;
using static Tanzu.Toolkit.Services.OutputHandler.OutputHandler;

namespace Tanzu.Toolkit.Services.CfCli
{
    public interface ICfCliService
    {
        string GetOAuthToken();
        DetailedResult TargetApi(string apiAddress, bool skipSsl);
        Task<DetailedResult> AuthenticateAsync(string username, SecureString password);
        DetailedResult ExecuteCfCliCommand(string arguments, string workingDir = null);
        Task<DetailedResult<List<Org>>> GetOrgsAsync();
        Task<DetailedResult<List<Space>>> GetSpacesAsync();
        Task<DetailedResult> TargetOrg(string orgName);
        Task<DetailedResult> TargetSpace(string spaceName);
        Task<DetailedResult<List<App>>> GetAppsAsync();
        Task<DetailedResult> StopAppByNameAsync(string appName);
        Task<DetailedResult> StartAppByNameAsync(string appName);
        Task<DetailedResult> DeleteAppByNameAsync(string appName, bool removeMappedRoutes = true);
        Task<DetailedResult> PushAppAsync(string appName, StdOutDelegate stdOutCallback, StdErrDelegate stdErrCallback, string appDir, string buildpack = null, string stack = null);
        Task<Version> GetApiVersion();
        Task<DetailedResult<string>> GetRecentAppLogs(string appName);
        void ClearCachedAccessToken();
    }
}