using System;
using System.Collections.Generic;
using System.Security;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Tanzu.Toolkit.CloudFoundryApiClient;
using Tanzu.Toolkit.CloudFoundryApiClient.Models.AppsResponse;
using Tanzu.Toolkit.CloudFoundryApiClient.Models.OrgsResponse;
using Tanzu.Toolkit.CloudFoundryApiClient.Models.SpacesResponse;
using Tanzu.Toolkit.Models;
using Tanzu.Toolkit.Services;
using Tanzu.Toolkit.Services.CfCli;
using Tanzu.Toolkit.Services.CloudFoundry;
using Tanzu.Toolkit.Services.Dialog;
using Tanzu.Toolkit.Services.ErrorDialog;
using Tanzu.Toolkit.Services.FileLocator;
using Tanzu.Toolkit.Services.Logging;
using static Tanzu.Toolkit.Services.OutputHandler.OutputHandler;

namespace Tanzu.Toolkit.Services.CloudFoundry
{
    public class CloudFoundryService : ICloudFoundryService
    {
        internal const string EmptyOutputDirMessage = "Unable to locate app files; project output directory is empty. (Has your project already been compiled?)";
        internal const string CcApiVersionUndetectableErrTitle = "Unable to detect Cloud Controller API version.";
        internal const string CcApiVersionUndetectableErrMsg = "Failed to detect which version of the Cloud Controller API is being run on the provided instance; some features of this extension may not work properly.";

        private readonly ICfApiClient _cfApiClient;
        private readonly ICfCliService _cfCliService;
        private readonly IFileLocatorService _fileLocatorService;
        private readonly IErrorDialog _dialogService;
        private readonly ILogger _logger;

        public string LoginFailureMessage { get; } = "Login failed.";
        public Dictionary<string, CloudFoundryInstance> CloudFoundryInstances { get; internal set; }
        public CloudFoundryInstance ActiveCloud { get; set; }

        public CloudFoundryService(IServiceProvider services)
        {
            CloudFoundryInstances = new Dictionary<string, CloudFoundryInstance>();

            _cfApiClient = services.GetRequiredService<ICfApiClient>();
            _cfCliService = services.GetRequiredService<ICfCliService>();
            _fileLocatorService = services.GetRequiredService<IFileLocatorService>();
            _dialogService = services.GetRequiredService<IErrorDialog>();

            var logSvc = services.GetRequiredService<ILoggingService>();
            _logger = logSvc.Logger;
        }

        public void AddCloudFoundryInstance(string name, string apiAddress, string accessToken)
        {
            if (CloudFoundryInstances.ContainsKey(name))
            {
                throw new Exception($"The name {name} already exists.");
            }

            CloudFoundryInstances.Add(name, new CloudFoundryInstance(name, apiAddress, accessToken));
        }

        public void RemoveCloudFoundryInstance(string name)
        {
            if (CloudFoundryInstances.ContainsKey(name))
            {
                CloudFoundryInstances.Remove(name);
            }
        }

        public async Task<ConnectResult> ConnectToCFAsync(string targetApiAddress, string username, SecureString password, string httpProxy, bool skipSsl)
        {
            if (string.IsNullOrEmpty(targetApiAddress))
            {
                throw new ArgumentException(nameof(targetApiAddress));
            }

            if (string.IsNullOrEmpty(username))
            {
                throw new ArgumentException(nameof(username));
            }

            if (password == null)
            {
                throw new ArgumentNullException(nameof(password));
            }

            try
            {
                DetailedResult targetResult = _cfCliService.TargetApi(targetApiAddress, skipSsl);
                bool unableToExecuteTargetCmd = targetResult.CmdDetails == null;

                if (unableToExecuteTargetCmd)
                {
                    throw new Exception(
                        message: LoginFailureMessage,
                        innerException: new Exception(
                            message: $"Unable to connect to CF CLI"));
                }
                else if (!targetResult.Succeeded)
                {
                    throw new Exception(
                        message: LoginFailureMessage,
                        innerException: new Exception(
                            message: $"Unable to target api at this address: {targetApiAddress}",
                            innerException: new Exception(targetResult.CmdDetails.StdErr)));
                }

                DetailedResult authResult = await _cfCliService.AuthenticateAsync(username, password);
                bool unableToExecuteAuthCmd = authResult.CmdDetails == null;

                if (unableToExecuteAuthCmd)
                {
                    throw new Exception(
                       message: LoginFailureMessage,
                       innerException: new Exception(
                           message: $"Unable to connect to Cf CLI"));
                }
                else if (!authResult.Succeeded)
                {
                    throw new Exception(
                       message: LoginFailureMessage,
                       innerException: new Exception(
                           message: $"Unable to authenticate user \"{username}\"",
                           innerException: new Exception(authResult.CmdDetails.StdErr)));
                }

                await MatchCliVersionToApiVersion();

                string accessToken = _cfCliService.GetOAuthToken();

                if (!string.IsNullOrEmpty(accessToken))
                {
                    return new ConnectResult(true, null, accessToken);
                }

                throw new Exception(LoginFailureMessage);
            }
            catch (Exception e)
            {
                var errorMessages = new List<string>();
                FormatExceptionMessage(e, errorMessages);
                var errorMessage = string.Join(Environment.NewLine, errorMessages.ToArray());
                return new ConnectResult(false, errorMessage, null);
            }
        }

        /// <summary>
        /// Requests orgs from <see cref="CfApiClient"/> using access token from <see cref="CfCliService"/>. 
        /// <para>
        /// If any exceptions are thrown when trying to retrieve orgs, this method will clear the cached
        /// access token on <see cref="CfCliService"/> and attempt to retrieve the orgs again using a 
        /// fresh access token.
        /// </para>
        /// </summary>
        /// <param name="cf"></param>
        /// <param name="skipSsl"></param>
        public async Task<DetailedResult<List<CloudFoundryOrganization>>> GetOrgsForCfInstanceAsync(CloudFoundryInstance cf, bool skipSsl = true, int retryAmount = 1)
        {
            List<Org> orgsFromApi;
            var orgsToReturn = new List<CloudFoundryOrganization>();

            string apiAddress = cf.ApiAddress;

            var accessToken = _cfCliService.GetOAuthToken();
            if (accessToken == null)
            {
                var msg = $"CloudFoundryService attempted to get orgs for '{apiAddress}' but was unable to look up an access token.";
                _logger.Error(msg);

                return new DetailedResult<List<CloudFoundryOrganization>>()
                {
                    Succeeded = false,
                    Explanation = msg,
                };
            }

            try
            {
                orgsFromApi = await _cfApiClient.ListOrgs(apiAddress, accessToken);
            }
            catch (Exception originalException)
            {
                if (retryAmount > 0)
                {
                    _logger.Information($"GetOrgsForCfInstanceAsync caught an exception when trying to retrieve orgs: {originalException.Message}. About to clear the cached access token & try again ({retryAmount} retry attempts remaining).");
                    _cfCliService.ClearCachedAccessToken();
                    retryAmount -= 1;
                    return await GetOrgsForCfInstanceAsync(cf, skipSsl, retryAmount);
                }
                else
                {
                    var msg = $"{originalException.Message}. See logs for more details.";
                    _logger.Error(msg);

                    return new DetailedResult<List<CloudFoundryOrganization>>()
                    {
                        Succeeded = false,
                        Explanation = msg,
                    };
                }
            }

            foreach (Org org in orgsFromApi)
            {
                if (org.Name == null)
                {
                    _logger.Error("CloudFoundryService.GetOrgsForCfInstanceAsync encountered an org without a name; omitting it from the returned list of orgs.");
                }
                else if (org.Guid == null)
                {
                    _logger.Error("CloudFoundryService.GetOrgsForCfInstanceAsync encountered an org without a guid; omitting it from the returned list of orgs.");
                }
                else
                {
                    orgsToReturn.Add(new CloudFoundryOrganization(org.Name, org.Guid, cf));
                }
            }

            return new DetailedResult<List<CloudFoundryOrganization>>()
            {
                Succeeded = true,
                Content = orgsToReturn,
            };
        }

        /// <summary>
        /// Requests spaces for <paramref name="org"/> using access token from <see cref="CfCliService"/>.
        /// <para>
        /// If any exceptions are thrown when trying to retrieve spaces, this method will clear the cached
        /// access token on <see cref="CfCliService"/> and attempt to retrieve the spaces again using a 
        /// fresh access token.
        /// </para>
        /// </summary>
        /// <param name="org"></param>
        /// <param name="skipSsl"></param>
        public async Task<DetailedResult<List<CloudFoundrySpace>>> GetSpacesForOrgAsync(CloudFoundryOrganization org, bool skipSsl = true, int retryAmount = 1)
        {
            List<Space> spacesFromApi;
            var spacesToReturn = new List<CloudFoundrySpace>();

            string apiAddress = org.ParentCf.ApiAddress;

            var accessToken = _cfCliService.GetOAuthToken();
            if (accessToken == null)
            {
                var msg = $"CloudFoundryService attempted to get spaces for '{org.OrgName}' but was unable to look up an access token.";
                _logger.Error(msg);

                return new DetailedResult<List<CloudFoundrySpace>>()
                {
                    Succeeded = false,
                    Explanation = msg,
                };
            }

            try
            {
                spacesFromApi = await _cfApiClient.ListSpacesForOrg(apiAddress, accessToken, org.OrgId);
            }
            catch (Exception originalException)
            {
                if (retryAmount > 0)
                {
                    _logger.Information($"GetSpacesForOrgAsync caught an exception when trying to retrieve spaces: {originalException.Message}. About to clear the cached access token & try again ({retryAmount} retry attempts remaining).");
                    _cfCliService.ClearCachedAccessToken();
                    retryAmount -= 1;
                    return await GetSpacesForOrgAsync(org, skipSsl, retryAmount);
                }
                else
                {
                    var msg = $"{originalException.Message}. See logs for more details.";
                    _logger.Error(msg);

                    return new DetailedResult<List<CloudFoundrySpace>>()
                    {
                        Succeeded = false,
                        Explanation = msg,
                    };
                }
            }

            foreach (Space space in spacesFromApi)
            {
                if (space.Name == null)
                {
                    _logger.Error("CloudFoundryService.GetSpacesForOrgAsync encountered a space without a name; omitting it from the returned list of spaces.");
                }
                else if (space.Guid == null)
                {
                    _logger.Error("CloudFoundryService.GetSpacesForOrgAsync encountered a space without a guid; omitting it from the returned list of spaces.");
                }
                else
                {
                    spacesToReturn.Add(new CloudFoundrySpace(space.Name, space.Guid, org));
                }
            }

            return new DetailedResult<List<CloudFoundrySpace>>()
            {
                Succeeded = true,
                Content = spacesToReturn,
            };
        }

        /// <summary>
        /// Requests apps for <paramref name="space"/> using access token from <see cref="CfCliService"/>.
        /// <para>
        /// If any exceptions are thrown when trying to retrieve apps, this method will clear the cached
        /// access token on <see cref="CfCliService"/> and attempt to retrieve the apps again using a 
        /// fresh access token.
        /// </para>
        /// </summary>
        /// <param name="space"></param>
        /// <param name="skipSsl"></param>
        public async Task<DetailedResult<List<CloudFoundryApp>>> GetAppsForSpaceAsync(CloudFoundrySpace space, bool skipSsl = true, int retryAmount = 1)
        {
            List<App> appsFromApi;
            var appsToReturn = new List<CloudFoundryApp>();

            string apiAddress = space.ParentOrg.ParentCf.ApiAddress;

            var accessToken = _cfCliService.GetOAuthToken();
            if (accessToken == null)
            {
                var msg = $"CloudFoundryService attempted to get apps for '{space.SpaceName}' but was unable to look up an access token.";
                _logger.Error(msg);

                return new DetailedResult<List<CloudFoundryApp>>()
                {
                    Succeeded = false,
                    Explanation = msg,
                };
            }

            try
            {
                appsFromApi = await _cfApiClient.ListAppsForSpace(apiAddress, accessToken, space.SpaceId);
            }
            catch (Exception originalException)
            {
                if (retryAmount > 0)
                {
                    _logger.Information($"GetAppsForSpaceAsync caught an exception when trying to retrieve apps: {originalException.Message}. About to clear the cached access token & try again ({retryAmount} retry attempts remaining).");
                    _cfCliService.ClearCachedAccessToken();
                    retryAmount -= 1;
                    return await GetAppsForSpaceAsync(space, skipSsl, retryAmount);
                }
                else
                {
                    var msg = $"{originalException.Message}. See logs for more details.";
                    _logger.Error(msg);

                    return new DetailedResult<List<CloudFoundryApp>>()
                    {
                        Succeeded = false,
                        Explanation = msg,
                    };
                }
            }

            foreach (App app in appsFromApi)
            {
                if (app.Name == null)
                {
                    _logger.Error("CloudFoundryService.GetAppsForSpaceAsync encountered an app without a name; omitting it from the returned list of apps.");
                }
                else if (app.Guid == null)
                {
                    _logger.Error("CloudFoundryService.GetAppsForSpaceAsync encountered an app without a guid; omitting it from the returned list of apps.");
                }
                else
                {
                    appsToReturn.Add(new CloudFoundryApp(app.Name, app.Guid, space, app.State.ToUpper()));
                }
            }

            return new DetailedResult<List<CloudFoundryApp>>()
            {
                Succeeded = true,
                Content = appsToReturn,
            };
        }

        /// <summary>
        /// Stop <paramref name="app"/> using token from <see cref="CfCliService"/>.
        /// <para>
        /// If any exceptions are thrown when trying to stop, this method will clear the cached
        /// access token on <see cref="CfCliService"/> and attempt to stop the app again using 
        /// a fresh access token.
        /// </para>
        /// </summary>
        /// <param name="app"></param>
        /// <param name="skipSsl"></param>
        public async Task<DetailedResult> StopAppAsync(CloudFoundryApp app, bool skipSsl = true, int retryAmount = 1)
        {
            bool appWasStopped;

            string apiAddress = app.ParentSpace.ParentOrg.ParentCf.ApiAddress;

            var accessToken = _cfCliService.GetOAuthToken();
            if (accessToken == null)
            {
                var msg = $"CloudFoundryService attempted to stop app '{app.AppName}' but was unable to look up an access token.";
                _logger.Error(msg);

                return new DetailedResult
                {
                    Succeeded = false,
                    Explanation = msg,
                };
            }

            try
            {
                appWasStopped = await _cfApiClient.StopAppWithGuid(apiAddress, accessToken, app.AppId);
            }
            catch (Exception originalException)
            {
                if (retryAmount > 0)
                {
                    _logger.Information($"StopAppAsync caught an exception when trying to stop app '{app.AppName}': {originalException.Message}. About to clear the cached access token & try again ({retryAmount} retry attempts remaining).");
                    _cfCliService.ClearCachedAccessToken();
                    retryAmount -= 1;
                    return await StopAppAsync(app, skipSsl, retryAmount);
                }
                else
                {
                    var msg = $"{originalException.Message}. See logs for more details.";

                    _logger.Error(msg);

                    return new DetailedResult
                    {
                        Succeeded = false,
                        Explanation = msg,
                    };
                }
            }

            if (!appWasStopped)
            {
                var msg = $"Attempted to stop app '{app.AppName}' but it hasn't been stopped.";

                _logger.Error(msg);

                return new DetailedResult
                {
                    Succeeded = false,
                    Explanation = msg,
                };
            }

            app.State = "STOPPED";
            return new DetailedResult
            {
                Succeeded = true,
            };
        }

        /// <summary>
        /// Start <paramref name="app"/> using token from <see cref="CfCliService"/>.
        /// <para>
        /// If any exceptions are thrown when trying to start, this method will clear the cached
        /// access token on <see cref="CfCliService"/> and attempt to start the app again using 
        /// a fresh access token.
        /// </para>
        /// </summary>
        /// <param name="app"></param>
        /// <param name="skipSsl"></param>
        public async Task<DetailedResult> StartAppAsync(CloudFoundryApp app, bool skipSsl = true, int retryAmount = 1)
        {
            bool appWasStarted;

            string apiAddress = app.ParentSpace.ParentOrg.ParentCf.ApiAddress;

            var accessToken = _cfCliService.GetOAuthToken();
            if (accessToken == null)
            {
                var msg = $"CloudFoundryService attempted to start app '{app.AppName}' but was unable to look up an access token.";
                _logger.Error(msg);

                return new DetailedResult
                {
                    Succeeded = false,
                    Explanation = msg,
                };
            }

            try
            {
                appWasStarted = await _cfApiClient.StartAppWithGuid(apiAddress, accessToken, app.AppId);
            }
            catch (Exception originalException)
            {
                if (retryAmount > 0)
                {
                    _logger.Information($"StartAppAsync caught an exception when trying to start app '{app.AppName}': {originalException.Message}. About to clear the cached access token & try again ({retryAmount} retry attempts remaining).");
                    _cfCliService.ClearCachedAccessToken();
                    retryAmount -= 1;
                    return await StartAppAsync(app, skipSsl, retryAmount);
                }
                else
                {
                    var msg = $"{originalException.Message}. See logs for more details.";

                    _logger.Error(msg);

                    return new DetailedResult
                    {
                        Succeeded = false,
                        Explanation = msg,
                    };
                }
            }

            if (!appWasStarted)
            {
                var msg = $"Attempted to start app '{app.AppName}' but it hasn't been started.";

                _logger.Error(msg);

                return new DetailedResult
                {
                    Succeeded = false,
                    Explanation = msg,
                };
            }

            app.State = "STARTED";
            return new DetailedResult
            {
                Succeeded = true,
            };
        }

        /// <summary>
        /// Delete <paramref name="app"/> using token from <see cref="CfCliService"/>.
        /// <para>
        /// If any exceptions are thrown when trying to delete, this method will clear the cached
        /// access token on <see cref="CfCliService"/> and attempt to delete the app again using 
        /// a fresh access token.
        /// </para> 
        /// </summary>
        /// <param name="app"></param>
        /// <param name="skipSsl"></param>
        /// <param name="removeRoutes"></param>
        /// <param name="retryAmount"></param>
        /// <returns></returns>
        public async Task<DetailedResult> DeleteAppAsync(CloudFoundryApp app, bool skipSsl = true, bool removeRoutes = true, int retryAmount = 1)
        {
            bool appWasDeleted;

            string apiAddress = app.ParentSpace.ParentOrg.ParentCf.ApiAddress;

            var accessToken = _cfCliService.GetOAuthToken();
            if (accessToken == null)
            {
                var msg = $"CloudFoundryService attempted to delete app '{app.AppName}' but was unable to look up an access token.";
                _logger.Error(msg);

                return new DetailedResult
                {
                    Succeeded = false,
                    Explanation = msg,
                };
            }

            try
            {
                appWasDeleted = await _cfApiClient.DeleteAppWithGuid(apiAddress, accessToken, app.AppId);

                if (!appWasDeleted)
                {
                    var msg = $"Attempted to delete app '{app.AppName}' but it hasn't been deleted.";

                    _logger.Error(msg);

                    return new DetailedResult
                    {
                        Succeeded = false,
                        Explanation = msg,
                    };
                }
            }
            catch (Exception originalException)
            {
                if (retryAmount > 0)
                {
                    _logger.Information($"StartAppAsync caught an exception when trying to start app '{app.AppName}': {originalException.Message}. About to clear the cached access token & try again ({retryAmount} retry attempts remaining).");
                    _cfCliService.ClearCachedAccessToken();
                    retryAmount -= 1;
                    return await DeleteAppAsync(app, skipSsl, removeRoutes, retryAmount);
                }
                else
                {
                    var msg = $"{originalException.Message}. See logs for more details.";

                    _logger.Error(msg);

                    return new DetailedResult
                    {
                        Succeeded = false,
                        Explanation = msg,
                    };
                }
            }

            app.State = "DELETED";
            return new DetailedResult
            {
                Succeeded = true,
            };
        }

        public async Task<DetailedResult> DeployAppAsync(CloudFoundryInstance targetCf, CloudFoundryOrganization targetOrg, CloudFoundrySpace targetSpace, string appName, string appProjPath, bool fullFrameworkDeployment, StdOutDelegate stdOutCallback, StdErrDelegate stdErrCallback)
        {
            if (!_fileLocatorService.DirContainsFiles(appProjPath))
            {
                return new DetailedResult(false, EmptyOutputDirMessage);
            }

            string buildpack = null;
            string stack = null;
            if (fullFrameworkDeployment)
            {
                buildpack = "hwc_buildpack";
                stack = "windows";
            }

            DetailedResult cfPushResult = await _cfCliService.PushAppAsync(appName, targetOrg.OrgName, targetSpace.SpaceName, stdOutCallback, stdErrCallback, appProjPath, buildpack, stack);

            if (!cfPushResult.Succeeded)
            {
                _logger.Error($"Successfully targeted org '{targetOrg.OrgName}' and space '{targetSpace.SpaceName}' but app deployment failed at the `cf push` stage.\n{cfPushResult.Explanation}");
                return new DetailedResult(false, cfPushResult.Explanation);
            }

            return new DetailedResult(true, $"App successfully deploying to org '{targetOrg.OrgName}', space '{targetSpace.SpaceName}'...");
        }

        public async Task<DetailedResult<string>> GetRecentLogs(CloudFoundryApp app)
        {
            return await _cfCliService.GetRecentAppLogs(app.AppName, app.ParentSpace.ParentOrg.OrgName, app.ParentSpace.SpaceName);
        }

        private void FormatExceptionMessage(Exception ex, List<string> message)
        {
            if (ex is AggregateException aex)
            {
                foreach (Exception iex in aex.InnerExceptions)
                {
                    FormatExceptionMessage(iex, message);
                }
            }
            else
            {
                message.Add(ex.Message);

                if (ex.InnerException != null)
                {
                    FormatExceptionMessage(ex.InnerException, message);
                }
            }
        }

        private async Task MatchCliVersionToApiVersion()
        {
            Version apiVersion = await _cfCliService.GetApiVersion();
            if (apiVersion == null)
            {
                _fileLocatorService.CliVersion = 7;
                _dialogService.DisplayErrorDialog(CcApiVersionUndetectableErrTitle, CcApiVersionUndetectableErrMsg);
            }
            else
            {
                switch (apiVersion.Major)
                {
                    case 2:
                        if (apiVersion < new Version("2.128.0"))
                        {
                            _fileLocatorService.CliVersion = 6;

                            string errorTitle = "API version not supported";
                            string errorMsg = "Detected a Cloud Controller API version lower than the minimum supported version (2.128.0); some features of this extension may not work as expected for the given instance.";

                            _logger.Information(errorMsg);
                            _dialogService.DisplayErrorDialog(errorTitle, errorMsg);
                        }
                        else if (apiVersion < new Version("2.150.0"))
                        {
                            _fileLocatorService.CliVersion = 6;
                        }
                        else
                        {
                            _fileLocatorService.CliVersion = 7;
                        }

                        break;

                    case 3:
                        if (apiVersion < new Version("3.63.0"))
                        {
                            _fileLocatorService.CliVersion = 6;

                            string errorTitle = "API version not supported";
                            string errorMsg = "Detected a Cloud Controller API version lower than the minimum supported version (3.63.0); some features of this extension may not work as expected for the given instance.";

                            _logger.Information(errorMsg);
                            _dialogService.DisplayErrorDialog(errorTitle, errorMsg);
                        }
                        else if (apiVersion < new Version("3.85.0"))
                        {
                            _fileLocatorService.CliVersion = 6;
                        }
                        else
                        {
                            _fileLocatorService.CliVersion = 7;
                        }

                        break;

                    default:
                        _fileLocatorService.CliVersion = 7;
                        _logger.Information($"Detected an unexpected Cloud Controller API version: {apiVersion}. CLI version has been set to 7 by default.");

                        break;
                }
            }
        }
    }
}