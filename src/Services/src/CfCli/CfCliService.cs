using Microsoft.Extensions.DependencyInjection;
using Serilog;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Tanzu.Toolkit.Services.CfCli.Models;
using Tanzu.Toolkit.Services.CfCli.Models.Apps;
using Tanzu.Toolkit.Services.CfCli.Models.Orgs;
using Tanzu.Toolkit.Services.CfCli.Models.Spaces;
using Tanzu.Toolkit.Services.CmdProcess;
using Tanzu.Toolkit.Services.FileLocator;
using Tanzu.Toolkit.Services.Logging;
using static Tanzu.Toolkit.Services.OutputHandler.OutputHandler;

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
        internal const string _getOrgsRequestPath = "GET /v2/organizations";
        internal const string _getSpacesRequestPath = "GET /v2/spaces";
        internal const string _getAppsRequestPath = "GET /v2/spaces"; // not a typo; app info returned from /v2/spaces/:guid/apps
        internal const string _invalidRefreshTokenError = "The token expired, was revoked, or the token ID is incorrect. Please log back in to re-authenticate.";

        private readonly IFileLocatorService _fileLocatorService;
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
            _fileLocatorService = services.GetRequiredService<IFileLocatorService>();
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
                var content = result.CmdDetails.StdOut.ToLower();

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

                            if (result.CmdDetails.ExitCode != 0)
                            {
                                _logger.Error($"GetOAuthToken failed: {result}");
                                return null;
                            }

                            var accessToken = FormatToken(result.CmdDetails.StdOut);

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

        public async Task<DetailedResult<List<Org>>> GetOrgsAsync()
        {
            DetailedResult cmdResult = null;

            try
            {
                string args = $"{_getOrgsCmd} -v"; // -v prints api request details to stdout
                cmdResult = await RunCfCommandAsync(args);

                if (!cmdResult.Succeeded)
                {
                    _logger.Error($"GetOrgsAsync() failed during InvokeCfCliAsync: {cmdResult}");

                    return new DetailedResult<List<Org>>(
                        content: null,
                        succeeded: false,
                        explanation: _requestErrorMsg,
                        cmdDetails: cmdResult.CmdDetails);
                }

                /* break early & skip json parsing if output contains 'No orgs found' */
                string content = cmdResult.CmdDetails.StdOut;
                string contentEnding = content.Substring(content.Length - 20);
                if (contentEnding.Contains("No orgs found"))
                {
                    return new DetailedResult<List<Org>>(
                        content: new List<Org>(),
                        succeeded: true,
                        explanation: null,
                        cmdDetails: cmdResult.CmdDetails);
                }

                var orgResponsePages = GetJsonResponsePages<OrgsApiV2ResponsePage>(cmdResult.CmdDetails.StdOut, _getOrgsRequestPath);

                /* check for unsuccessful json parsing */
                if (orgResponsePages == null)
                {
                    _logger.Error($"GetOrgsAsync() failed during response parsing. Used this delimeter: '{_getOrgsRequestPath}' to parse through: {cmdResult.CmdDetails.StdOut}");

                    return new DetailedResult<List<Org>>(
                        content: null,
                        succeeded: false,
                        explanation: _jsonParsingErrorMsg,
                        cmdDetails: cmdResult.CmdDetails);
                }

                var orgsList = new List<Org>();
                foreach (OrgsApiV2ResponsePage responsePage in orgResponsePages)
                {
                    foreach (Org org in responsePage.Resources)
                    {
                        orgsList.Add(org);
                    }
                }

                return new DetailedResult<List<Org>>(orgsList, true, null, cmdResult.CmdDetails);
            }
            catch (Exception e)
            {
                _logger.Error($"GetOrgsAsync() failed due to an unpredicted exception: {e}.");

                return new DetailedResult<List<Org>>(null, false, e.Message, cmdResult?.CmdDetails);
            }
        }

        public async Task<DetailedResult<List<Space>>> GetSpacesAsync()
        {
            DetailedResult cmdResult = null;

            try
            {
                string args = $"{_getSpacesCmd} -v"; // -v prints api request details to stdout
                cmdResult = await RunCfCommandAsync(args);

                if (!cmdResult.Succeeded)
                {
                    _logger.Error($"GetSpacesAsync() failed during InvokeCfCliAsync: {cmdResult}");

                    return new DetailedResult<List<Space>>(
                        content: null,
                        succeeded: false,
                        explanation: _requestErrorMsg,
                        cmdDetails: cmdResult.CmdDetails);
                }

                /* break early & skip json parsing if output contains 'No spaces found' */
                string content = cmdResult.CmdDetails.StdOut;
                string contentEnding = content.Substring(content.Length - 20);
                if (contentEnding.Contains("No spaces found"))
                {
                    return new DetailedResult<List<Space>>(
                        content: new List<Space>(),
                        succeeded: true,
                        explanation: null,
                        cmdDetails: cmdResult.CmdDetails);
                }

                var spaceResponsePages = GetJsonResponsePages<SpacesApiV2ResponsePage>(content, _getSpacesRequestPath);

                /* check for unsuccessful json parsing */
                if (spaceResponsePages == null)
                {
                    _logger.Error($"GetSpacesAsync() failed during response parsing. Used this delimeter: '{_getSpacesRequestPath}' to parse through: {cmdResult.CmdDetails.StdOut}");

                    return new DetailedResult<List<Space>>(
                        content: null,
                        succeeded: false,
                        explanation: _jsonParsingErrorMsg,
                        cmdDetails: cmdResult.CmdDetails);
                }

                var spacesList = new List<Space>();
                foreach (SpacesApiV2ResponsePage responsePage in spaceResponsePages)
                {
                    foreach (Space space in responsePage.Resources)
                    {
                        spacesList.Add(space);
                    }
                }

                return new DetailedResult<List<Space>>(spacesList, true, null, cmdResult.CmdDetails);
            }
            catch (Exception e)
            {
                _logger.Error($"GetSpacesAsync() failed due to an unpredicted exception: {e}.");

                return new DetailedResult<List<Space>>(null, false, e.Message, cmdResult?.CmdDetails);
            }
        }

        public async Task<DetailedResult<List<App>>> GetAppsAsync()
        {
            DetailedResult cmdResult = null;

            try
            {
                string args = $"{_getAppsCmd} -v"; // -v prints api request details to stdout
                cmdResult = await RunCfCommandAsync(args);

                if (!cmdResult.Succeeded)
                {
                    _logger.Error($"GetAppsAsync() failed during InvokeCfCliAsync: {cmdResult}");

                    return new DetailedResult<List<App>>(
                        content: null,
                        succeeded: false,
                        explanation: _requestErrorMsg,
                        cmdDetails: cmdResult.CmdDetails);
                }

                /* break early & skip json parsing if output contains 'No apps found' */
                string content = cmdResult.CmdDetails.StdOut;
                string contentEnding = content.Substring(content.Length - 20);
                if (contentEnding.Contains("No apps found"))
                {
                    return new DetailedResult<List<App>>(
                        content: new List<App>(),
                        succeeded: true,
                        explanation: null,
                        cmdDetails: cmdResult.CmdDetails);
                }

                var appsResponses = GetJsonResponsePages<AppsApiV2Response>(content, _getAppsRequestPath);

                /* check for unsuccessful json parsing */
                if (appsResponses == null ||
                    (appsResponses.Count > 0 && appsResponses[0].Guid == null && appsResponses[0].Name == null && appsResponses[0].Services == null && appsResponses[0].Apps == null))
                {
                    _logger.Error($"GetAppsAsync() failed during response parsing. Used this delimeter: '{_getAppsRequestPath}' to parse through: {cmdResult.CmdDetails.StdOut}");

                    return new DetailedResult<List<App>>(
                        content: null,
                        succeeded: false,
                        explanation: _jsonParsingErrorMsg,
                        cmdDetails: cmdResult.CmdDetails);
                }

                var appsList = new List<App>();
                foreach (AppsApiV2Response response in appsResponses)
                {
                    foreach (App app in response.Apps)
                    {
                        appsList.Add(app);
                    }
                }

                return new DetailedResult<List<App>>(appsList, true, null, cmdResult.CmdDetails);
            }
            catch (Exception e)
            {
                _logger.Error($"GetAppsAsync() failed due to an unpredicted exception: {e}.");

                return new DetailedResult<List<App>>(null, false, e.Message, cmdResult?.CmdDetails);
            }
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

        /// <summary>
        /// Invokes `cf push` with the specified app name. Assumes the proper org & space have already been targeted.
        /// </summary>
        /// <param name="appName"></param>
        /// <param name="stdOutCallback"></param>
        /// <param name="stdErrCallback"></param>
        /// <param name="appDir"></param>
        public async Task<DetailedResult> PushAppAsync(string appName, string orgName, string spaceName, StdOutDelegate stdOutCallback, StdErrDelegate stdErrCallback, string appDirPath, string buildpack = null, string stack = null, string startCommand = null, string manifestPath = null)
        {
            string args = $"push \"{appName}\"";

            if (buildpack != null)
            {
                args += $" -b {buildpack}";
            }

            if (stack != null)
            {
                args += $" -s {stack}";
            }

            if (startCommand != null)
            {
                args += $" -c \"{startCommand}\"";
            }

            if (manifestPath != null)
            {
                args += $" -f \"{manifestPath}\"";
            }

            if (appDirPath != null)
            {
                args += $" -p \"{appDirPath}\"";
            }

            Task<DetailedResult> deployTask;
            lock (_cfEnvironmentLock)
            {
                var targetOrgResult = TargetOrg(orgName);
                if (!targetOrgResult.Succeeded)
                {
                    string msg = $"Unable to deploy app '{appName}'; failed to target org '{orgName}'.\n{targetOrgResult.Explanation}";
                    _logger.Error(msg);
                    return new DetailedResult(false, explanation: msg, targetOrgResult.CmdDetails);
                }

                var targetSpaceResult = TargetSpace(spaceName);
                if (!targetSpaceResult.Succeeded)
                {
                    string msg = $"Unable to deploy app '{appName}'; failed to target space '{spaceName}'.\n{targetSpaceResult.Explanation}";
                    _logger.Error(msg);
                    return new DetailedResult(false, explanation: msg, targetSpaceResult.CmdDetails);
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
                    return new DetailedResult<string>(null, targetOrgResult.Succeeded, targetOrgResult.Explanation, targetOrgResult.CmdDetails);
                }

                var targetSpaceResult = TargetSpace(spaceName);
                if (!targetSpaceResult.Succeeded)
                {
                    return new DetailedResult<string>(null, targetSpaceResult.Succeeded, targetSpaceResult.Explanation, targetSpaceResult.CmdDetails);
                }

                logsTask = Task.Run(async () => await RunCfCommandAsync(args));
            }

            var recentLogsResult = await logsTask;

            ThrowIfResultIndicatesInvalidRefreshToken(recentLogsResult);

            var content = recentLogsResult.CmdDetails.StdOut;
            var cmdDetails = recentLogsResult.CmdDetails;
            var explanation = recentLogsResult.Explanation;
            bool succeeded = recentLogsResult.Succeeded;

            return new DetailedResult<string>(content, succeeded, explanation, cmdDetails);
        }

        /// <summary>
        /// Invoke a CF CLI command using the <see cref="CmdProcessService"/>.
        /// This method is synchronous, meaning it can be used within a lock statement.
        /// </summary>
        /// <param name="arguments"></param>
        /// <param name="stdOutCallback"></param>
        /// <param name="stdErrCallback"></param>
        /// <param name="workingDir"></param>
        /// <returns>A <see cref="DetailedResult"/> containing the results of the CF command.</returns>
        public DetailedResult ExecuteCfCliCommand(string arguments, string workingDir = null)
        {
            string pathToCfExe = _fileLocatorService.FullPathToCfExe;
            if (string.IsNullOrEmpty(pathToCfExe))
            {
                return new DetailedResult(false, _cfExePathErrorMsg);
            }

            var envVars = new Dictionary<string, string>
            {
                { "CF_HOME", ConfigFilePath }
            };

            ICmdProcessService cmdProcessService = Services.GetRequiredService<ICmdProcessService>();
            CmdResult result = cmdProcessService.RunExecutable(pathToCfExe, arguments, workingDir, envVars);

            if (result.ExitCode == 0)
            {
                return new DetailedResult(succeeded: true, cmdDetails: result);
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

            return new DetailedResult(false, reason, cmdDetails: result);
        }

        /// <summary>
        /// Initiate a CF CLI command process by invoking the <see cref="CmdProcessService"/>.
        /// This method is asynchronous, meaning it cannot be used within a lock statement.
        /// </summary>
        /// <param name="arguments"></param>
        /// <param name="stdOutCallback"></param>
        /// <param name="stdErrCallback"></param>
        /// <param name="workingDir"></param>
        /// <returns>An awaitable <see cref="Task"/> which will return a <see cref="DetailedResult"/> containing the results of the CF command.</returns>
        internal async Task<DetailedResult> RunCfCommandAsync(string arguments, StdOutDelegate stdOutCallback = null, StdErrDelegate stdErrCallback = null, string workingDir = null)
        {
            string pathToCfExe = _fileLocatorService.FullPathToCfExe;
            if (string.IsNullOrEmpty(pathToCfExe))
            {
                return new DetailedResult(false, $"Unable to locate cf.exe.");
            }

            var envVars = new Dictionary<string, string>
            {
                { "CF_HOME", ConfigFilePath }
            };

            ICmdProcessService cmdProcessService = Services.GetRequiredService<ICmdProcessService>();
            CmdResult result = await Task.Run(() => cmdProcessService.RunExecutable(pathToCfExe, arguments, workingDir, envVars, stdOutCallback, stdErrCallback));

            if (result.ExitCode == 0)
            {
                return new DetailedResult(succeeded: true, cmdDetails: result);
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

            return new DetailedResult(false, reason, cmdDetails: result);
        }

        /// <summary>
        /// Tries to parse string content from a CF CLI v6 response (1 or more pages).
        /// </summary>
        /// <typeparam name="TResponse">Type of data contract into which the json response should be deserialized.</typeparam>
        /// <param name="content"></param>
        /// <param name="requestFilter"></param>
        /// <returns>
        ///     <para>A list of <typeparamref name="ResponseType"/> pages if successful.</para>
        ///     <para><c>null</c> otherwise.</para>
        /// </returns>
        internal List<TResponse> GetJsonResponsePages<TResponse>(string content, string requestFilter)
        {
            try
            {
                var jsonResponses = new List<TResponse>();

                if (!content.Contains("REQUEST") || !content.Contains("RESPONSE") || !content.Contains(requestFilter))
                {
                    _logger.Error($"Api response parsing failed; content either does not contain 'REQUEST', 'RESPONSE' or '{requestFilter}'. Content: {content}");
                    return null;
                }

                /* separate content into individual request/response pairs */
                string[] requestResponsePairs = Regex.Split(content, pattern: "(?=REQUEST)");

                /* filter out extraneous requests */
                List<string> relevantRequests = requestResponsePairs
                    .Where(s => s.Contains(requestFilter))
                    .ToList();

                foreach (string requestResponsePair in relevantRequests)
                {
                    if (!string.IsNullOrWhiteSpace(requestResponsePair))
                    {
                        /* separate request from response */
                        string[] reqRes = Regex.Split(requestResponsePair, pattern: "(?=RESPONSE)");

                        string responseContent = reqRes[1];

                        /* look for any part of the response content that resembles json */
                        if (responseContent.Contains("{") && responseContent.Contains("}"))
                        {
                            int substringStart = responseContent.IndexOf('{');
                            int substringEnd = responseContent.LastIndexOf('}');
                            int substringLength = substringEnd - substringStart + 1;

                            string speculativeJsonStr = responseContent.Substring(
                                substringStart,
                                substringLength);

                            /* try converting json-like string to `ResponseType` instance */
                            var responsePage = JsonSerializer.Deserialize<TResponse>(speculativeJsonStr);

                            jsonResponses.Add(responsePage);
                        }
                        else
                        {
                            _logger.Error($"Api response parsing failed; content either does not contain '{{', or '}}'. Content: {content}");

                            return null;
                        }
                    }
                }

                if (jsonResponses.Count == 1)
                {
                    if (jsonResponses[0] is ApiV2Response response
                        && response.NextUrl == null
                        && response.PrevUrl == null
                        && response.TotalPages == 0
                        && response.TotalResults == 0)
                    {
                        return null;
                    }
                }

                return jsonResponses;
            }
            catch (Exception ex)
            {
                _logger.Error($"Api response parsing failed due to an unpredicted exception: {ex}.");

                return null;
            }
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
            if (result != null && result.CmdDetails != null && result.CmdDetails.StdErr != null && result.CmdDetails.StdErr.Contains(_invalidRefreshTokenError))
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
