using Microsoft.Extensions.DependencyInjection;
using Serilog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IdentityModel.Tokens.Jwt;
using System.Security;
using System.Threading.Tasks;
using Tanzu.Toolkit.Services.CommandProcess;
using Tanzu.Toolkit.Services.File;
using Tanzu.Toolkit.Services.Logging;

namespace Tanzu.Toolkit.Services.CfCli
{
    public class CfCliService : ICfCliService
    {
        /* ERROR MESSAGE CONSTANTS */
        internal const string _cfExePathErrorMsg = "Unable to locate cf.exe.";
        internal const string _requestErrorMsg = "An error occurred while requesting content from the Cloud Foundry API.";
        internal const string _jsonParsingErrorMsg = "Unable to parse response from Cloud Foundry API.";

        /* CF CLI CONSTANTS */
        internal const string _getApiVersionCmd = "api";
        internal const string _getCliVersionCmd = "version";
        internal const string _getOAuthTokenCmd = "oauth-token";
        internal const string _targetApiCmd = "api";
        internal const string _authenticateCmd = "auth";
        internal const string _targetOrgCmd = "target -o";
        internal const string _targetSpaceCmd = "target -s";
        internal const string _getOrgsCmd = "orgs";
        internal const string _getSpacesCmd = "spaces";
        internal const string _getAppsCmd = "apps";
        internal const string _stopAppCmd = "stop";
        internal const string _startAppCmd = "start";
        internal const string _deleteAppCmd = "delete -f"; // -f avoids confirmation prompt
        internal const string _invalidRefreshTokenError = "The token expired, was revoked, or the token ID is incorrect. Please log back in to re-authenticate.";
        internal const string _logoutCmd = "logout";

        private readonly IFileService _fileService;
        private readonly ILogger _logger;

        private object _cfEnvironmentLock = new object();

        private volatile string _cachedAccessToken = null;
        private DateTime _accessTokenExpiration = new DateTime(0);

        private string ConfigFilePath { get; }

        private IServiceProvider Services { get; set; }

        public CfCliService(string configFilePath, IServiceProvider services)
        {
            ConfigFilePath = configFilePath;
            Services = services;
            _fileService = services.GetRequiredService<IFileService>();
            var logSvc = services.GetRequiredService<ILoggingService>();
            _logger = logSvc.Logger;
        }

        /// <summary>
        /// Invokes the `cf api` command to access the currently-running version of the Cloud Controller API.
        /// </summary>
        /// <returns>
        /// <code>int[] versionNumbers</code>Array of integers: versionNumbers[0] = major version, versionNumbers[1] = minor, etc.
        /// </returns>
        public async Task<Version> GetApiVersion()
        {
            DetailedResult result = await RunCfCommandAsync(_getApiVersionCmd);
            if (!result.Succeeded)
            {
                return null;
            }

            try
            {
                const string versionLabel = "api version:";
                var content = result.CmdResult.StdOut.ToLower();

                var versionString = content.Substring(content.IndexOf(versionLabel))
                    .Replace("\n", string.Empty)
                    .Replace(versionLabel, string.Empty)
                    .Replace(" ", string.Empty);

                var version = new Version(versionString);
                return version;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Retrieves an access token to be used for authentication with Cloud Foundry UAA.
        /// <para>
        /// A cached value of the access token will be returned as long as the expiration time of the token has not yet been reached.
        /// If there is no cached value for the access token or the expiration time of the cached token has been reached, this method 
        /// will invoke the CF CLI oauth-token command to acquire a new access token. Invoking the CF CLI oauth-token command will 
        /// attempt to renew the access token using the refresh token stored by the CF CLI.
        /// </para>
        /// <para>
        /// The cached value for the access token can be cleared using <see cref="ClearCachedAccessToken"/>; this will force a fresh 
        /// access token to be obtained using the refresh token via the CF CLI oauth-token command.
        /// </para>
        /// <exception cref="InvalidRefreshTokenException">Throws <see cref="InvalidRefreshTokenException"/> if a fresh access token
        /// is unobtainable to due an invalid refresh token (this would occur once the refresh token reaches the end of its 
        /// prescribed lifetime).</exception>
        /// </summary>
        /// <returns><see cref="string"/> accessToken on success.<para>null on failure.</para></returns>
        public string GetOAuthToken()
        {
            if (_cachedAccessToken == null || _accessTokenExpiration == null || DateTime.Compare(DateTime.Now, _accessTokenExpiration) >= 0)
            {
                lock (_cfEnvironmentLock)
                {
                    // double-check just in case the last thread to access this block changed these values
                    // (prevents scenario where many threads wait for lock once token expires, then *all* try to refresh token)
                    if (_cachedAccessToken == null || _accessTokenExpiration == null || DateTime.Compare(DateTime.Now, _accessTokenExpiration) >= 0)
                    {
                        try
                        {
                            DetailedResult result = ExecuteCfCliCommand(_getOAuthTokenCmd);

                            if (result == null)
                            {
                                return null;
                            }

                            ThrowIfResultIndicatesInvalidRefreshToken(result);

                            if (result.CmdResult.ExitCode != 0)
                            {
                                _logger.Error($"GetOAuthToken failed: {result}");
                                return null;
                            }

                            var accessToken = FormatToken(result.CmdResult.StdOut);

                            var handler = new JwtSecurityTokenHandler();
                            var jsonToken = handler.ReadToken(accessToken);
                            var token = jsonToken as JwtSecurityToken;

                            _cachedAccessToken = accessToken;
                            _accessTokenExpiration = token.ValidTo;
                        }
                        catch (Exception ex)
                        {
                            if (ex is InvalidRefreshTokenException)
                            {
                                throw ex;
                            }

                            _logger.Error("Something went wrong while attempting to renew access token {TokenException}", ex);
                        }
                    }
                }
            }

            return _cachedAccessToken;
        }

        public void ClearCachedAccessToken()
        {
            lock (_cfEnvironmentLock)
            {
                _cachedAccessToken = null;
            }
        }

        public DetailedResult TargetApi(string apiAddress, bool skipSsl)
        {
            DetailedResult result;

            lock (_cfEnvironmentLock)
            {
                string args = $"{_targetApiCmd} {apiAddress}{(skipSsl ? " --skip-ssl-validation" : string.Empty)}";
                result = ExecuteCfCliCommand(args);
            }

            if (result == null || !result.Succeeded)
            {
                _logger.Error($"TargetApi({apiAddress}, {skipSsl}) failed: {result}");

                if (result.CmdResult.StdErr != null && result.CmdResult.StdErr.Contains("certificate has expired or is not yet valid"))
                {
                    result.FailureType = FailureType.InvalidCertificate;
                }
            }

            return result;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0059:Unnecessary assignment of a value", Justification = "Null assigment is meant to clear plain text password from memory")]
        public async Task<DetailedResult> AuthenticateAsync(string username, SecureString password)
        {
            string passwordStr = new System.Net.NetworkCredential(string.Empty, password).Password;

            string args = $"{_authenticateCmd} {username} {passwordStr}";
            DetailedResult result = await RunCfCommandAsync(args);

            /* Erase pw from memory */
            passwordStr = null;
            password.Clear();
            password.Dispose();

            if (!result.Succeeded)
            {
                _logger.Error($"AuthenticateAsync({username}, ***) failed: {result}");
            }

            return result;
        }

        /// <summary>
        /// Invokes a `cf target -o` command using the CF CLI.
        /// <para>This method is not thread-safe; if two threads invoke it simultaneously, the 
        /// first one to change the targeted org may have its efforts undone when the second 
        /// one changes the targeted org. </para>
        /// <para>To avoid race conditions around the "targeted org" value, make sure to invoke 
        /// this method only after acquiring the <see cref="_cfEnvironmentLock"/>.</para>
        /// <exception cref="InvalidRefreshTokenException">Throws <see cref="InvalidRefreshTokenException"/> if a fresh access token
        /// is unobtainable to due an invalid refresh token (this would occur once the refresh token reaches the end of its 
        /// prescribed lifetime).</exception>
        /// </summary>
        /// <param name="orgName"></param>
        /// <returns></returns>
        public DetailedResult TargetOrg(string orgName)
        {
            string args = $"{_targetOrgCmd} \"{orgName}\"";
            DetailedResult result = ExecuteCfCliCommand(args);

            ThrowIfResultIndicatesInvalidRefreshToken(result);

            if (result == null || !result.Succeeded)
            {
                _logger.Error("TargetOrg({OrgName}) failed: {TargetOrgResult}", orgName, result);
            }

            return result;
        }

        /// <summary>
        /// Invokes a `cf target -s` command using the CF CLI.
        /// <para>This method is not thread-safe; if two threads invoke it simultaneously, the 
        /// first one to change the targeted space may have its efforts undone when the second 
        /// one changes the targeted space. </para>
        /// <para>To avoid race conditions around the "targeted space" value, make sure to invoke 
        /// this method only after acquiring the <see cref="_cfEnvironmentLock"/>.</para>
        /// <exception cref="InvalidRefreshTokenException">Throws <see cref="InvalidRefreshTokenException"/> if a fresh access token
        /// is unobtainable to due an invalid refresh token (this would occur once the refresh token reaches the end of its 
        /// prescribed lifetime).</exception>
        /// </summary>
        /// <param name="spaceName"></param>
        /// <returns></returns>
        public DetailedResult TargetSpace(string spaceName)
        {
            string args = $"{_targetSpaceCmd} \"{spaceName}\"";
            DetailedResult result = ExecuteCfCliCommand(args);

            ThrowIfResultIndicatesInvalidRefreshToken(result);

            if (result == null || !result.Succeeded)
            {
                _logger.Error("TargetSpace({SpaceName}) failed: {TargetSpaceResult}", spaceName, result);
            }

            return result;
        }

        public async Task<DetailedResult> StopAppByNameAsync(string appName)
        {
            string args = $"{_stopAppCmd} \"{appName}\"";
            DetailedResult result = await RunCfCommandAsync(args);

            if (!result.Succeeded)
            {
                _logger.Error($"StopAppByNameAsync({appName}) failed during InvokeCfCliAsync: {result}");
            }

            return result;
        }

        public async Task<DetailedResult> StartAppByNameAsync(string appName)
        {
            string args = $"{_startAppCmd} \"{appName}\"";
            DetailedResult result = await RunCfCommandAsync(args);

            if (!result.Succeeded)
            {
                _logger.Error($"StartAppByNameAsync({appName}) failed during InvokeCfCliAsync: {result}");
            }

            return result;
        }

        public async Task<DetailedResult> DeleteAppByNameAsync(string appName, bool removeMappedRoutes = true)
        {
            string args = $"{_deleteAppCmd} \"{appName}\"{(removeMappedRoutes ? " -r" : string.Empty)}";
            DetailedResult result = await RunCfCommandAsync(args);

            if (!result.Succeeded)
            {
                _logger.Error($"DeleteAppByNameAsync({appName}, {removeMappedRoutes}) failed during InvokeCfCliAsync: {result}");
            }

            return result;
        }

        public async Task<DetailedResult> PushAppAsync(string manifestPath, string appDirPath, string orgName, string spaceName, Action<string> stdOutCallback, Action<string> stdErrCallback)
        {
            string args = $"push -f \"{manifestPath}\"";

            if (!_fileService.FileExists(manifestPath))
            {
                string msg = $"Unable to deploy app; no manifest file found at '{manifestPath}'";
                _logger.Error(msg);
                return new DetailedResult(false, explanation: msg);
            }

            Task<DetailedResult> deployTask;
            lock (_cfEnvironmentLock)
            {
                var targetOrgResult = TargetOrg(orgName);
                if (!targetOrgResult.Succeeded)
                {
                    string msg = $"Unable to deploy app from '{manifestPath}'; failed to target org '{orgName}'.\n{targetOrgResult.Explanation}";
                    _logger.Error(msg);
                    return new DetailedResult(false, explanation: msg, targetOrgResult.CmdResult);
                }

                var targetSpaceResult = TargetSpace(spaceName);
                if (!targetSpaceResult.Succeeded)
                {
                    string msg = $"Unable to deploy app from '{manifestPath}'; failed to target space '{spaceName}'.\n{targetSpaceResult.Explanation}";
                    _logger.Error(msg);
                    return new DetailedResult(false, explanation: msg, targetSpaceResult.CmdResult);
                }

                deployTask = Task.Run(async () => await RunCfCommandAsync(args, stdOutCallback, stdErrCallback, appDirPath));
            }

            var deployResult = await deployTask;

            ThrowIfResultIndicatesInvalidRefreshToken(deployResult);

            return deployResult;
        }

        public async Task<DetailedResult<string>> GetRecentAppLogs(string appName, string orgName, string spaceName)
        {
            var args = $"logs \"{appName}\" --recent";

            Task<DetailedResult> logsTask;

            lock (_cfEnvironmentLock)
            {
                var targetOrgResult = TargetOrg(orgName);
                if (!targetOrgResult.Succeeded)
                {
                    return new DetailedResult<string>(null, targetOrgResult.Succeeded, targetOrgResult.Explanation, targetOrgResult.CmdResult);
                }

                var targetSpaceResult = TargetSpace(spaceName);
                if (!targetSpaceResult.Succeeded)
                {
                    return new DetailedResult<string>(null, targetSpaceResult.Succeeded, targetSpaceResult.Explanation, targetSpaceResult.CmdResult);
                }

                logsTask = Task.Run(async () => await RunCfCommandAsync(args));
            }

            var recentLogsResult = await logsTask;

            ThrowIfResultIndicatesInvalidRefreshToken(recentLogsResult);

            var content = recentLogsResult.CmdResult.StdOut;
            var cmdDetails = recentLogsResult.CmdResult;
            var explanation = recentLogsResult.Explanation;
            bool succeeded = recentLogsResult.Succeeded;

            return new DetailedResult<string>(content, succeeded, explanation, cmdDetails);
        }

        public DetailedResult<Process> StreamAppLogs(string appName, string orgName, string spaceName, Action<string> stdOutCallback, Action<string> stdErrCallback)
        {
            var args = $"logs \"{appName}\"";
            Process logsProcess = null;

            lock (_cfEnvironmentLock)
            {
                var targetOrgResult = TargetOrg(orgName);
                if (!targetOrgResult.Succeeded)
                {
                    return new DetailedResult<Process>
                    {
                        Succeeded = false,
                        Explanation = $"Unable to target org '{orgName}'",
                    };
                }

                var targetSpaceResult = TargetSpace(spaceName);
                if (!targetSpaceResult.Succeeded)
                {
                    return new DetailedResult<Process>
                    {
                        Succeeded = false,
                        Explanation = $"Unable to target space '{spaceName}'",
                    };
                }

                logsProcess = StartCfProcess(args, stdOutCallback, stdErrCallback);
            }

            return logsProcess == null
                ? new DetailedResult<Process>
                {
                    Succeeded = false,
                    Explanation = $"Failed to start logs stream process for app {appName}",
                }
                : new DetailedResult<Process>
                {
                    Succeeded = true,
                    Content = logsProcess,
                };
        }

        public async Task<DetailedResult> LoginWithSsoPasscode(string apiAddress, string passcode)
        {
            string args = $"login -a \"{apiAddress}\" --sso-passcode \"{passcode}\"";

            var processCancellationTriggers = new List<string> { "OK", "Invalid passcode" };

            Task<DetailedResult> loginTask;
            lock (_cfEnvironmentLock)
            {
                loginTask = Task.Run(async () => await RunCfCommandAsync(args, cancellationTriggers: processCancellationTriggers));
            }

            return await loginTask;
        }

        public DetailedResult Logout()
        {
            return ExecuteCfCliCommand(_logoutCmd);
        }

        /// <summary>
        /// Invoke a CF CLI command using the <see cref="CommandProcessService"/>.
        /// This method is synchronous, meaning it can be used within a lock statement.
        /// </summary>
        /// <param name="arguments"></param>
        /// <param name="stdOutCallback"></param>
        /// <param name="stdErrCallback"></param>
        /// <param name="workingDir"></param>
        /// <returns>A <see cref="DetailedResult"/> containing the results of the CF command.</returns>
        internal DetailedResult ExecuteCfCliCommand(string arguments, string workingDir = null)
        {
            string pathToCfExe = _fileService.FullPathToCfExe;
            if (string.IsNullOrEmpty(pathToCfExe))
            {
                return new DetailedResult(false, _cfExePathErrorMsg);
            }

            var envVars = new Dictionary<string, string>
            {
                { "CF_HOME", ConfigFilePath }
            };

            ICommandProcessService cmdProcessService = Services.GetRequiredService<ICommandProcessService>();
            CommandResult result = cmdProcessService.RunExecutable(pathToCfExe, arguments, workingDir, envVars);

            if (result.ExitCode == 0)
            {
                return new DetailedResult(succeeded: true, cmdResult: result);
            }

            string reason = result.StdErr;
            if (string.IsNullOrEmpty(result.StdErr))
            {
                if (result.StdOut.Contains("FAILED"))
                {
                    reason = result.StdOut;
                }
                else
                {
                    reason = $"Unable to execute `cf {arguments}`.";
                }
            }

            return new DetailedResult(false, reason, cmdResult: result);
        }

        /// <summary>
        /// Initiate a CF CLI command process by invoking the <see cref="CommandProcessService"/>.
        /// This method is asynchronous, meaning it cannot be used within a lock statement.
        /// </summary>
        /// <param name="arguments"></param>
        /// <param name="stdOutCallback"></param>
        /// <param name="stdErrCallback"></param>
        /// <param name="workingDir"></param>
        /// <param name="cancellationTriggers"></param>
        /// <returns>An awaitable <see cref="Task"/> which will return a <see cref="DetailedResult"/> containing the results of the CF command.</returns>
        internal async Task<DetailedResult> RunCfCommandAsync(string arguments, Action<string> stdOutCallback = null, Action<string> stdErrCallback = null, string workingDir = null, List<string> cancellationTriggers = null)
        {
            string pathToCfExe = _fileService.FullPathToCfExe;
            if (string.IsNullOrEmpty(pathToCfExe))
            {
                return new DetailedResult(false, $"Unable to locate cf.exe.");
            }

            var envVars = new Dictionary<string, string>
            {
                { "CF_HOME", ConfigFilePath }
            };

            ICommandProcessService cmdProcessService = Services.GetRequiredService<ICommandProcessService>();
            CommandResult result = await Task.Run(() => cmdProcessService.RunExecutable(pathToCfExe, arguments, workingDir, envVars, stdOutCallback, stdErrCallback, processCancelTriggers: cancellationTriggers));

            if (result.ExitCode == 0)
            {
                return new DetailedResult(succeeded: true, cmdResult: result);
            }

            string reason = result.StdErr;
            if (string.IsNullOrEmpty(result.StdErr))
            {
                if (result.StdOut.Contains("FAILED"))
                {
                    reason = result.StdOut;
                }
                else
                {
                    reason = $"Unable to execute `cf {arguments}`.";
                }
            }

            return new DetailedResult(false, reason, cmdResult: result);
        }

        private Process StartCfProcess(string arguments, Action<string> stdOutCallback = null, Action<string> stdErrCallback = null, string workingDir = null, List<string> cancellationTriggers = null)
        {
            string pathToCfExe = _fileService.FullPathToCfExe;
            if (string.IsNullOrEmpty(pathToCfExe))
            {
                _logger.Error($"CfCliService tried to start command 'cf {arguments}' but was unable to locate cf.exe.");
                return null;
            }

            var envVars = new Dictionary<string, string>
            {
                { "CF_HOME", ConfigFilePath }
            };

            ICommandProcessService cmdProcessService = Services.GetRequiredService<ICommandProcessService>();
            return cmdProcessService.StartProcess(
                pathToCfExe,
                arguments,
                workingDir,
                envVars,
                stdOutCallback,
                stdErrCallback,
                processCancelTriggers: cancellationTriggers);
        }

        private string FormatToken(string tokenStr)
        {
            tokenStr = tokenStr.Replace("\n", "");
            if (tokenStr.StartsWith("bearer "))
            {
                tokenStr = tokenStr.Remove(0, 7);
            }

            return tokenStr;
        }

        private static void ThrowIfResultIndicatesInvalidRefreshToken(DetailedResult result)
        {
            if (result != null && result.CmdResult != null && result.CmdResult.StdErr != null && result.CmdResult.StdErr.Contains(_invalidRefreshTokenError))
            {
                throw new InvalidRefreshTokenException();
            }
        }
    }

    public class InvalidRefreshTokenException : Exception
    {
        public InvalidRefreshTokenException()
        {
        }
    }
}
