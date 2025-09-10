using Microsoft.Extensions.DependencyInjection;
using Serilog;
using System;
using System.Diagnostics;
using System.Security;
using System.Threading.Tasks;
using Tanzu.Toolkit.Services.Logging;

namespace Tanzu.Toolkit.Services.CfCli
{
    public class OfflineCfCliService : ICfCliService
    {
        private const string _targetApiCmd = "api";
        private const string _authenticateCmd = "auth";

        private readonly ILogger _logger;

        private readonly object _cfEnvironmentLock = new object();

        private IServiceProvider Services { get; set; }

        public OfflineCfCliService(IServiceProvider services)
        {
            Services = services;

            try
            {
                var logSvc = services.GetRequiredService<ILoggingService>();
                _logger = logSvc.Logger;
            }
            catch (Exception ex)
            {
                _logger?.Error("Unable to construct {ClassName} due to an unattainable service: {ServiceException}", nameof(CfCliService), ex);
            }
        }

        public async Task<Version> GetApiVersionAsync()
        {
            await Task.CompletedTask;
            throw new NotImplementedException();
        }

        public string GetOAuthToken()
        {
            return "FakeToken";
        }

        public DetailedResult TargetApi(string apiAddress, bool skipSsl)
        {
            return new DetailedResult(true, "This is a fake success message");
        }

        public async Task<DetailedResult> AuthenticateAsync(string username, SecureString password)
        {
            await Task.CompletedTask;
            return new DetailedResult(true, "This is a fake success message");
        }

        public DetailedResult TargetOrg(string orgName)
        {
            return new DetailedResult(true, "This is a fake success message");
        }

        public DetailedResult TargetSpace(string spaceName)
        {
            return new DetailedResult(true, "This is a fake success message");
        }

        public async Task<DetailedResult> StopAppByNameAsync(string appName)
        {
            await Task.CompletedTask;
            return new DetailedResult(true, "This is a fake success message");
        }

        public async Task<DetailedResult> StartAppByNameAsync(string appName)
        {
            await Task.CompletedTask;
            return new DetailedResult(true, "This is a fake success message");
        }

        public async Task<DetailedResult> DeleteAppByNameAsync(string appName, bool removeMappedRoutes = true)
        {
            await Task.CompletedTask;
            return new DetailedResult(true, "This is a fake success message");
        }

        public void ClearCachedAccessToken()
        {
            throw new NotImplementedException();
        }

        public async Task<DetailedResult> PushAppAsync(string manifestPath, string appDirPath, string orgName, string spaceName, Action<string> stdOutCallback,
            Action<string> stdErrCallback)
        {
            await Task.CompletedTask;
            return new DetailedResult(true, "This is a fake success message");
        }

        public async Task<DetailedResult<string>> GetRecentAppLogsAsync(string appName, string orgName, string spaceName)
        {
            await Task.CompletedTask;
            return new DetailedResult<string>("This is a fake log entry", true, "This is a fake success message");
        }

        public DetailedResult<Process> StreamAppLogs(string appName, string orgName, string spaceName, Action<string> stdOutCallback, Action<string> stdErrCallback)
        {
            throw new NotImplementedException();
        }

        public async Task<DetailedResult> LoginWithSSOPasscodeAsync(string apiAddress, string passcode)
        {
            await Task.CompletedTask;
            return new DetailedResult(true, "This is a fake success message");
        }

        public async Task<DetailedResult> ExecuteSSHCommandAsync(string appName, string orgName, string spaceName, string sshCommand)
        {
            await Task.CompletedTask;
            return new DetailedResult(true, "This is a fake success message");
        }

        public DetailedResult Logout()
        {
            return new DetailedResult(true, "This is a fake success message");
        }
    }
}