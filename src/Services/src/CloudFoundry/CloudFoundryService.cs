using Microsoft.Extensions.DependencyInjection;
using Serilog;
using System;
using System.Collections.Generic;
using System.Security;
using System.Threading.Tasks;
using Tanzu.Toolkit.CloudFoundryApiClient;
using Tanzu.Toolkit.CloudFoundryApiClient.Models;
using Tanzu.Toolkit.CloudFoundryApiClient.Models.AppsResponse;
using Tanzu.Toolkit.CloudFoundryApiClient.Models.OrgsResponse;
using Tanzu.Toolkit.CloudFoundryApiClient.Models.SpacesResponse;
using Tanzu.Toolkit.Models;
using Tanzu.Toolkit.Services.CfCli;
using Tanzu.Toolkit.Services.ErrorDialog;
using Tanzu.Toolkit.Services.File;
using Tanzu.Toolkit.Services.Logging;
using YamlDotNet.Serialization;
using static Tanzu.Toolkit.Services.OutputHandler.OutputHandler;

namespace Tanzu.Toolkit.Services.CloudFoundry
{
    public class CloudFoundryService : ICloudFoundryService
    {
        internal const string EmptyOutputDirMessage = "Unable to locate app files; project output directory is empty. (Has your project already been compiled?)";
        internal const string CcApiVersionUndetectableErrTitle = "Unable to detect Cloud Controller API version.";
        internal const string CcApiVersionUndetectableErrMsg = "Failed to detect which version of the Cloud Controller API is being run on the provided instance; some features of this extension may not work properly.";
        internal const string LoginFailureMessage = "Login failed.";

        private readonly ICfApiClient _cfApiClient;
        private readonly ICfCliService _cfCliService;
        private readonly IFileService _fileService;
        private readonly IErrorDialog _dialogService;
        private readonly ILogger _logger;

        public CloudFoundryService(IServiceProvider services)
        {
            _cfApiClient = services.GetRequiredService<ICfApiClient>();
            _cfCliService = services.GetRequiredService<ICfCliService>();
            _fileService = services.GetRequiredService<IFileService>();
            _dialogService = services.GetRequiredService<IErrorDialog>();

            var logSvc = services.GetRequiredService<ILoggingService>();
            _logger = logSvc.Logger;
        }

        public async Task<ConnectResult> ConnectToCFAsync(string targetApiAddress, string username, SecureString password, bool skipSsl)
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
                bool unableToExecuteTargetCmd = targetResult.CmdResult == null;

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
                            innerException: new Exception(targetResult.CmdResult.StdErr)));
                }

                DetailedResult authResult = await _cfCliService.AuthenticateAsync(username, password);
                bool unableToExecuteAuthCmd = authResult.CmdResult == null;

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
                           innerException: new Exception(authResult.CmdResult.StdErr)));
                }

                await MatchCliVersionToApiVersion();

                return new ConnectResult(true, null);
            }
            catch (Exception e)
            {
                var errorMessages = new List<string>();
                FormatExceptionMessage(e, errorMessages);
                var errorMessage = string.Join(Environment.NewLine, errorMessages.ToArray());
                return new ConnectResult(false, errorMessage);
            }
        }

        /// <summary>
        /// Requests orgs from <see cref="CfApiClient"/> using access token from <see cref="CfCliService"/>. 
        /// <para>
        /// If any exceptions are thrown when trying to retrieve orgs, this method will clear the cached
        /// access token on <see cref="CfCliService"/> and attempt to retrieve the orgs again using a 
        /// fresh access token.
        /// </para>
        /// <para>
        /// An <see cref="InvalidRefreshTokenException"/> caught while trying to attain an access
        /// token will cause this method to return a <see cref="DetailedResult"/> with a 
        /// FailureType of <see cref="FailureType.InvalidRefreshToken"/>.
        /// </para>
        /// </summary>
        /// <param name="cf"></param>
        /// <param name="skipSsl"></param>
        public async Task<DetailedResult<List<CloudFoundryOrganization>>> GetOrgsForCfInstanceAsync(CloudFoundryInstance cf, bool skipSsl = true, int retryAmount = 1)
        {
            string apiAddress = cf.ApiAddress;

            string accessToken;
            try
            {
                accessToken = _cfCliService.GetOAuthToken();
            }
            catch (InvalidRefreshTokenException)
            {
                var msg = "Unable to retrieve orgs for '{CfName}' because the connection has expired. Please log back in to re-authenticate.";
                _logger.Information(msg, cf.InstanceName);

                return new DetailedResult<List<CloudFoundryOrganization>>
                {
                    Succeeded = false,
                    Explanation = msg.Replace("{CfName}", cf.InstanceName),
                    Content = null,
                    FailureType = FailureType.InvalidRefreshToken,
                };
            }

            if (accessToken == null)
            {
                _logger.Error("CloudFoundryService attempted to get orgs for {apiAddress} but was unable to look up an access token.", apiAddress);

                return new DetailedResult<List<CloudFoundryOrganization>>()
                {
                    Succeeded = false,
                    Explanation = $"CloudFoundryService attempted to get orgs for '{apiAddress}' but was unable to look up an access token.",
                };
            }

            List<Org> orgsFromApi;
            try
            {
                orgsFromApi = await _cfApiClient.ListOrgs(apiAddress, accessToken);
            }
            catch (Exception originalException)
            {
                if (retryAmount > 0)
                {
                    _logger.Information("GetOrgsForCfInstanceAsync caught an exception when trying to retrieve orgs: {originalException}. About to clear the cached access token & try again ({retryAmount} retry attempts remaining).", originalException.Message, retryAmount);
                    _cfCliService.ClearCachedAccessToken();
                    retryAmount -= 1;
                    return await GetOrgsForCfInstanceAsync(cf, skipSsl, retryAmount);
                }
                else
                {
                    _logger.Error("{Error}. See logs for more details: toolkit-diagnostics.log", originalException.Message);

                    return new DetailedResult<List<CloudFoundryOrganization>>()
                    {
                        Succeeded = false,
                        Explanation = originalException.Message,
                    };
                }
            }

            var orgsToReturn = new List<CloudFoundryOrganization>();
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
        /// <para>
        /// An <see cref="InvalidRefreshTokenException"/> caught while trying to attain an access
        /// token will cause this method to return a <see cref="DetailedResult"/> with a 
        /// FailureType of <see cref="FailureType.InvalidRefreshToken"/>.
        /// </para>
        /// </summary>
        /// <param name="org"></param>
        /// <param name="skipSsl"></param>
        public async Task<DetailedResult<List<CloudFoundrySpace>>> GetSpacesForOrgAsync(CloudFoundryOrganization org, bool skipSsl = true, int retryAmount = 1)
        {
            string apiAddress = org.ParentCf.ApiAddress;

            string accessToken;
            try
            {
                accessToken = _cfCliService.GetOAuthToken();
            }
            catch (InvalidRefreshTokenException)
            {
                var msg = "Unable to retrieve spaces for '{OrgName}' because the connection to '{CfName}' has expired. Please log back in to re-authenticate.";
                _logger.Information(msg, org.OrgName, org.ParentCf.InstanceName);

                return new DetailedResult<List<CloudFoundrySpace>>
                {
                    Succeeded = false,
                    Explanation = msg.Replace("{OrgName}", org.OrgName).Replace("{CfName}", org.ParentCf.InstanceName),
                    Content = null,
                    FailureType = FailureType.InvalidRefreshToken,
                };
            }

            if (accessToken == null)
            {
                _logger.Error("CloudFoundryService attempted to get spaces for '{orgName}' but was unable to look up an access token.", org.OrgName);

                return new DetailedResult<List<CloudFoundrySpace>>()
                {
                    Succeeded = false,
                    Explanation = $"CloudFoundryService attempted to get spaces for '{org.OrgName}' but was unable to look up an access token.",
                };
            }

            List<Space> spacesFromApi;
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
                    _logger.Error("{Error}. See logs for more details: toolkit-diagnostics.log", originalException.Message);

                    return new DetailedResult<List<CloudFoundrySpace>>()
                    {
                        Succeeded = false,
                        Explanation = originalException.Message,
                    };
                }
            }

            var spacesToReturn = new List<CloudFoundrySpace>();
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
        /// <para>
        /// An <see cref="InvalidRefreshTokenException"/> caught while trying to attain an access
        /// token will cause this method to return a <see cref="DetailedResult"/> with a 
        /// FailureType of <see cref="FailureType.InvalidRefreshToken"/>.
        /// </para>
        /// </summary>
        /// <param name="space"></param>
        /// <param name="skipSsl"></param>
        public async Task<DetailedResult<List<CloudFoundryApp>>> GetAppsForSpaceAsync(CloudFoundrySpace space, bool skipSsl = true, int retryAmount = 1)
        {
            string apiAddress = space.ParentOrg.ParentCf.ApiAddress;

            string accessToken;
            try
            {
                accessToken = _cfCliService.GetOAuthToken();
            }
            catch (InvalidRefreshTokenException)
            {
                var msg = "Unable to retrieve apps for '{SpaceName}' because the connection to '{CfName}' has expired. Please log back in to re-authenticate.";
                _logger.Information(msg, space.SpaceName, space.ParentOrg.ParentCf.InstanceName);

                return new DetailedResult<List<CloudFoundryApp>>
                {
                    Succeeded = false,
                    Explanation = msg.Replace("{SpaceName}", space.SpaceName).Replace("CfName", space.ParentOrg.ParentCf.InstanceName),
                    Content = null,
                    FailureType = FailureType.InvalidRefreshToken,
                };
            }

            if (accessToken == null)
            {
                _logger.Error("CloudFoundryService attempted to get apps for '{spaceName}' but was unable to look up an access token.", space.SpaceName);

                return new DetailedResult<List<CloudFoundryApp>>()
                {
                    Succeeded = false,
                    Explanation = $"CloudFoundryService attempted to get apps for '{space.SpaceName}' but was unable to look up an access token.",
                };
            }

            List<App> appsFromApi;
            try
            {
                appsFromApi = await _cfApiClient.ListAppsForSpace(apiAddress, accessToken, space.SpaceId);
            }
            catch (Exception originalException)
            {
                if (retryAmount > 0)
                {
                    _logger.Information("GetAppsForSpaceAsync caught an exception when trying to retrieve apps: {originalException}. About to clear the cached access token & try again ({retryAmount} retry attempts remaining).", originalException.Message, retryAmount);
                    _cfCliService.ClearCachedAccessToken();
                    retryAmount -= 1;
                    return await GetAppsForSpaceAsync(space, skipSsl, retryAmount);
                }
                else
                {
                    _logger.Error("{Error}. See logs for more details: toolkit-diagnostics.log", originalException.Message);

                    return new DetailedResult<List<CloudFoundryApp>>()
                    {
                        Succeeded = false,
                        Explanation = originalException.Message,
                    };
                }
            }

            var appsToReturn = new List<CloudFoundryApp>();
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

        public async Task<DetailedResult<List<string>>> GetUniqueBuildpackNamesAsync(string apiAddress, int retryAmount = 1)
        {
            string accessToken;
            try
            {
                accessToken = _cfCliService.GetOAuthToken();
            }
            catch (InvalidRefreshTokenException)
            {
                var msg = "Unable to retrieve buildpacks because the connection has expired. Please log back in to re-authenticate.";
                _logger.Information(msg);

                return new DetailedResult<List<string>>
                {
                    Succeeded = false,
                    Explanation = msg,
                    Content = null,
                    FailureType = FailureType.InvalidRefreshToken,
                };
            }

            if (accessToken == null)
            {
                _logger.Error("CloudFoundryService attempted to get buildpacks but was unable to look up an access token.");

                return new DetailedResult<List<string>>()
                {
                    Succeeded = false,
                    Explanation = $"CloudFoundryService attempted to get buildpacks but was unable to look up an access token.",
                };
            }

            List<Buildpack> buildpacksFromApi;
            try
            {
                buildpacksFromApi = await _cfApiClient.ListBuildpacks(apiAddress, accessToken);
            }
            catch (Exception originalException)
            {
                if (retryAmount > 0)
                {
                    _logger.Information("GetUniqueBuildpackNamesAsync caught an exception when trying to retrieve buildpacks: {originalException}. About to clear the cached access token & try again ({retryAmount} retry attempts remaining).", originalException.Message, retryAmount);
                    _cfCliService.ClearCachedAccessToken();
                    retryAmount -= 1;
                    return await GetUniqueBuildpackNamesAsync(apiAddress, retryAmount);
                }
                else
                {
                    _logger.Error("{Error}. See logs for more details: toolkit-diagnostics.log", originalException.Message);

                    return new DetailedResult<List<string>>()
                    {
                        Succeeded = false,
                        Explanation = originalException.Message,
                    };
                }
            }

            var buildpackNames = new List<string>();

            foreach (Buildpack buildpack in buildpacksFromApi)
            {
                if (string.IsNullOrWhiteSpace(buildpack.Name))
                {
                    _logger.Error("CloudFoundryService.GetUniqueBuildpackNamesAsync encountered a buildpack without a name; omitting it from the returned list of buildpacks.");
                }
                else if (!buildpackNames.Contains(buildpack.Name))
                {
                    buildpackNames.Add(buildpack.Name);
                }
            }

            return new DetailedResult<List<string>>()
            {
                Succeeded = true,
                Content = buildpackNames,
            };
        }

        /// <summary>
        /// Stop <paramref name="app"/> using token from <see cref="CfCliService"/>.
        /// <para>
        /// If any exceptions are thrown when trying to stop, this method will clear the cached
        /// access token on <see cref="CfCliService"/> and attempt to stop the app again using 
        /// a fresh access token.
        /// </para>
        /// <para>
        /// An <see cref="InvalidRefreshTokenException"/> caught while trying to attain an access
        /// token will cause this method to return a <see cref="DetailedResult"/> with a 
        /// FailureType of <see cref="FailureType.InvalidRefreshToken"/>.
        /// </para>
        /// </summary>
        /// <param name="app"></param>
        /// <param name="skipSsl"></param>
        public async Task<DetailedResult> StopAppAsync(CloudFoundryApp app, bool skipSsl = true, int retryAmount = 1)
        {
            string apiAddress = app.ParentSpace.ParentOrg.ParentCf.ApiAddress;

            string accessToken;
            try
            {
                accessToken = _cfCliService.GetOAuthToken();
            }
            catch (InvalidRefreshTokenException)
            {
                var msg = "Unable to stop app '{AppName}' because the connection to '{CfName}' has expired. Please log back in to re-authenticate.";
                _logger.Information(msg, app.AppName, app.ParentSpace.ParentOrg.ParentCf.InstanceName);

                return new DetailedResult
                {
                    Succeeded = false,
                    Explanation = msg.Replace("{AppName}", app.AppName).Replace("CfName", app.ParentSpace.ParentOrg.ParentCf.InstanceName),
                    FailureType = FailureType.InvalidRefreshToken,
                };
            }

            if (accessToken == null)
            {
                _logger.Error("CloudFoundryService attempted to stop app '{appName}' but was unable to look up an access token.", app.AppName);

                return new DetailedResult
                {
                    Succeeded = false,
                    Explanation = $"CloudFoundryService attempted to stop app '{app.AppName}' but was unable to look up an access token.",
                };
            }

            bool appWasStopped;
            try
            {
                appWasStopped = await _cfApiClient.StopAppWithGuid(apiAddress, accessToken, app.AppId);
            }
            catch (Exception originalException)
            {
                if (retryAmount > 0)
                {
                    _logger.Information("StopAppAsync caught an exception when trying to stop app '{appName}': {originalException}. About to clear the cached access token & try again ({retryAmount} retry attempts remaining).", app.AppName, originalException.Message, retryAmount);
                    _cfCliService.ClearCachedAccessToken();
                    retryAmount -= 1;
                    return await StopAppAsync(app, skipSsl, retryAmount);
                }
                else
                {
                    _logger.Error("{Error}. See logs for more details: toolkit-diagnostics.log", originalException.Message);

                    return new DetailedResult
                    {
                        Succeeded = false,
                        Explanation = originalException.Message,
                    };
                }
            }

            if (!appWasStopped)
            {
                _logger.Error("Attempted to stop app '{appName}' but it hasn't been stopped.", app.AppName);

                return new DetailedResult
                {
                    Succeeded = false,
                    Explanation = $"Attempted to stop app '{app.AppName}' but it hasn't been stopped.",
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
        /// <para>
        /// An <see cref="InvalidRefreshTokenException"/> caught while trying to attain an access
        /// token will cause this method to return a <see cref="DetailedResult"/> with a 
        /// FailureType of <see cref="FailureType.InvalidRefreshToken"/>.
        /// </para>
        /// </summary>
        /// <param name="app"></param>
        /// <param name="skipSsl"></param>
        public async Task<DetailedResult> StartAppAsync(CloudFoundryApp app, bool skipSsl = true, int retryAmount = 1)
        {
            string apiAddress = app.ParentSpace.ParentOrg.ParentCf.ApiAddress;

            string accessToken;
            try
            {
                accessToken = _cfCliService.GetOAuthToken();
            }
            catch (InvalidRefreshTokenException)
            {
                var msg = "Unable to start app '{AppName}' because the connection to '{CfName}' has expired. Please log back in to re-authenticate.";
                _logger.Information(msg, app.AppName, app.ParentSpace.ParentOrg.ParentCf.InstanceName);

                return new DetailedResult
                {
                    Succeeded = false,
                    Explanation = msg.Replace("{AppName}", app.AppName).Replace("CfName", app.ParentSpace.ParentOrg.ParentCf.InstanceName),
                    FailureType = FailureType.InvalidRefreshToken,
                };
            }

            if (accessToken == null)
            {
                _logger.Error("CloudFoundryService attempted to start app '{appName}' but was unable to look up an access token.", app.AppName);

                return new DetailedResult
                {
                    Succeeded = false,
                    Explanation = $"CloudFoundryService attempted to start app '{app.AppName}' but was unable to look up an access token.",
                };
            }

            bool appWasStarted;
            try
            {
                appWasStarted = await _cfApiClient.StartAppWithGuid(apiAddress, accessToken, app.AppId);
            }
            catch (Exception originalException)
            {
                if (retryAmount > 0)
                {
                    _logger.Information("StartAppAsync caught an exception when trying to start app '{appName}': {originalException}. About to clear the cached access token & try again ({retryAmount} retry attempts remaining).", app.AppName, originalException.Message, retryAmount);
                    _cfCliService.ClearCachedAccessToken();
                    retryAmount -= 1;
                    return await StartAppAsync(app, skipSsl, retryAmount);
                }
                else
                {
                    _logger.Error("{Error}. See logs for more details: toolkit-diagnostics.log", originalException.Message);

                    return new DetailedResult
                    {
                        Succeeded = false,
                        Explanation = originalException.Message,
                    };
                }
            }

            if (!appWasStarted)
            {
                _logger.Error("Attempted to start app '{appName}' but it hasn't been started.", app.AppName);

                return new DetailedResult
                {
                    Succeeded = false,
                    Explanation = $"Attempted to start app '{app.AppName}' but it hasn't been started.",
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
        /// <para>
        /// An <see cref="InvalidRefreshTokenException"/> caught while trying to attain an access
        /// token will cause this method to return a <see cref="DetailedResult"/> with a 
        /// FailureType of <see cref="FailureType.InvalidRefreshToken"/>.
        /// </para>
        /// </summary>
        /// <param name="app"></param>
        /// <param name="skipSsl"></param>
        /// <param name="removeRoutes"></param>
        /// <param name="retryAmount"></param>
        /// <returns></returns>
        public async Task<DetailedResult> DeleteAppAsync(CloudFoundryApp app, bool skipSsl = true, bool removeRoutes = true, int retryAmount = 1)
        {
            string apiAddress = app.ParentSpace.ParentOrg.ParentCf.ApiAddress;

            string accessToken;
            try
            {
                accessToken = _cfCliService.GetOAuthToken();
            }
            catch (InvalidRefreshTokenException)
            {
                var msg = "Unable to delete app '{AppName}' because the connection to '{CfName}' has expired. Please log back in to re-authenticate.";
                _logger.Information(msg, app.AppName, app.ParentSpace.ParentOrg.ParentCf.InstanceName);

                return new DetailedResult
                {
                    Succeeded = false,
                    Explanation = msg.Replace("{AppName}", app.AppName).Replace("CfName", app.ParentSpace.ParentOrg.ParentCf.InstanceName),
                    FailureType = FailureType.InvalidRefreshToken,
                };
            }

            if (accessToken == null)
            {
                _logger.Error("CloudFoundryService attempted to delete app '{appName}' but was unable to look up an access token.", app.AppName);

                return new DetailedResult
                {
                    Succeeded = false,
                    Explanation = $"CloudFoundryService attempted to delete app '{app.AppName}' but was unable to look up an access token.",
                };
            }

            bool appWasDeleted;
            try
            {
                appWasDeleted = await _cfApiClient.DeleteAppWithGuid(apiAddress, accessToken, app.AppId);

                if (!appWasDeleted)
                {
                    _logger.Error("Attempted to delete app '{appName}' but it hasn't been deleted.", app.AppName);

                    return new DetailedResult
                    {
                        Succeeded = false,
                        Explanation = $"Attempted to delete app '{app.AppName}' but it hasn't been deleted.",
                    };
                }
            }
            catch (Exception originalException)
            {
                if (retryAmount > 0)
                {
                    _logger.Information("StartAppAsync caught an exception when trying to start app '{appName}': {originalException}. About to clear the cached access token & try again ({retryAmount} retry attempts remaining).", app.AppName, originalException.Message, retryAmount);
                    _cfCliService.ClearCachedAccessToken();
                    retryAmount -= 1;
                    return await DeleteAppAsync(app, skipSsl, removeRoutes, retryAmount);
                }
                else
                {
                    _logger.Error("{Error}. See logs for more details: toolkit-diagnostics.log", originalException.Message);

                    return new DetailedResult
                    {
                        Succeeded = false,
                        Explanation = originalException.Message,
                    };
                }
            }

            app.State = "DELETED";
            return new DetailedResult
            {
                Succeeded = true,
            };
        }

        public async Task<DetailedResult> DeployAppAsync(CloudFoundryInstance targetCf, CloudFoundryOrganization targetOrg, CloudFoundrySpace targetSpace, string appName, string pathToDeploymentDirectory, bool fullFrameworkDeployment, StdOutDelegate stdOutCallback, StdErrDelegate stdErrCallback, string stack, bool binaryDeployment, string projectName, string manifestPath = null, string buildpack = null)
        {
            if (!_fileService.DirContainsFiles(pathToDeploymentDirectory))
            {
                return new DetailedResult(false, EmptyOutputDirMessage);
            }

            string startCommand = null;

            if (fullFrameworkDeployment)
            {
                buildpack = "hwc_buildpack";
                stack = "windows";
            }

            DetailedResult cfPushResult;
            try
            {
                if (binaryDeployment)
                {
                    buildpack = "binary_buildpack";
                    startCommand = $"cmd /c .\\{projectName} --urls=http://*:%PORT%";

                    if (fullFrameworkDeployment)
                    {
                        startCommand = $"cmd /c .\\{projectName} --server.urls=http://*:%PORT%";
                    }

                    if (stack == "cflinuxfs3")
                    {
                        buildpack = "dotnet_core_buildpack";
                        startCommand = null;
                    }
                }

                cfPushResult = await _cfCliService.PushAppAsync(manifestPath, pathToDeploymentDirectory, targetOrg.OrgName, targetSpace.SpaceName, stdOutCallback, stdErrCallback);
            }
            catch (InvalidRefreshTokenException)
            {
                var msg = "Unable to deploy app '{AppName}' to '{CfName}' because the connection has expired. Please log back in to re-authenticate.";
                _logger.Information(msg, appName, targetCf.InstanceName);

                return new DetailedResult
                {
                    Succeeded = false,
                    Explanation = msg.Replace("{AppName}", appName).Replace("{CfName}", targetCf.InstanceName),
                    FailureType = FailureType.InvalidRefreshToken,
                };
            }

            if (!cfPushResult.Succeeded)
            {
                _logger.Error("Successfully targeted org '{targetOrgName}' and space '{targetSpaceName}' but app deployment failed at the `cf push` stage.\n{cfPushResult}", targetOrg.OrgName, targetSpace.SpaceName, cfPushResult.Explanation);
                return new DetailedResult(false, cfPushResult.Explanation);
            }

            return new DetailedResult(true, $"App successfully deploying to org '{targetOrg.OrgName}', space '{targetSpace.SpaceName}'...");
        }

        public async Task<DetailedResult<string>> GetRecentLogs(CloudFoundryApp app)
        {
            DetailedResult<string> logsResult;

            try
            {
                logsResult = await _cfCliService.GetRecentAppLogs(app.AppName, app.ParentSpace.ParentOrg.OrgName, app.ParentSpace.SpaceName);
            }
            catch (InvalidRefreshTokenException)
            {
                var msg = "Unable to retrieve app logs from '{AppName}' because the connection has expired. Please log back in to re-authenticate.";
                _logger.Information(msg, app.AppName);

                return new DetailedResult<string>
                {
                    Succeeded = false,
                    Explanation = msg.Replace("{AppName}", app.AppName),
                    Content = null,
                    FailureType = FailureType.InvalidRefreshToken,
                };
            }

            return logsResult;
        }

        public DetailedResult CreateManifestFile(string location, AppManifest manifest)
        {
            try
            {
                var serializer = new SerializerBuilder()
                    .WithNamingConvention(CfAppManifestNamingConvention.Instance)
                    .ConfigureDefaultValuesHandling(DefaultValuesHandling.OmitNull)
                    .Build();

                string ymlContents = serializer.Serialize(manifest);

                _fileService.WriteTextToFile(location, "---\n" + ymlContents);

                return new DetailedResult
                {
                    Succeeded = true
                };
            }
            catch (Exception ex)
            {
                return new DetailedResult
                {
                    Succeeded = false,
                    Explanation = ex.Message,
                };
            }
        }

        public DetailedResult<AppManifest> ParseManifestFile(string pathToManifestFile)
        {
            if (!_fileService.FileExists(pathToManifestFile))
            {
                return new DetailedResult<AppManifest>
                {
                    Succeeded = false,
                    Content = null,
                    Explanation = $"No file exists at {pathToManifestFile}",
                };
            }

            try
            {
                string manifestContents = _fileService.ReadFileContents(pathToManifestFile);

                var deserializer = new DeserializerBuilder()
                    .WithNamingConvention(CfAppManifestNamingConvention.Instance)
                    .Build();

                var manifest = deserializer.Deserialize<AppManifest>(manifestContents);

                return new DetailedResult<AppManifest>
                {
                    Succeeded = true,
                    Content = manifest,
                };
            }
            catch (Exception ex)
            {
                return new DetailedResult<AppManifest>
                {
                    Succeeded = false,
                    Content = null,
                    Explanation = ex.Message,
                };
            }
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
                _fileService.CliVersion = 7;
                _dialogService.DisplayErrorDialog(CcApiVersionUndetectableErrTitle, CcApiVersionUndetectableErrMsg);
            }
            else
            {
                switch (apiVersion.Major)
                {
                    case 2:
                        if (apiVersion < new Version("2.128.0"))
                        {
                            _fileService.CliVersion = 6;

                            string errorTitle = "API version not supported";
                            string errorMsg = "Detected a Cloud Controller API version lower than the minimum supported version (2.128.0); some features of this extension may not work as expected for the given instance.";

                            _logger.Information(errorMsg);
                            _dialogService.DisplayErrorDialog(errorTitle, errorMsg);
                        }
                        else if (apiVersion < new Version("2.150.0"))
                        {
                            _fileService.CliVersion = 6;
                        }
                        else
                        {
                            _fileService.CliVersion = 7;
                        }

                        break;

                    case 3:
                        if (apiVersion < new Version("3.63.0"))
                        {
                            _fileService.CliVersion = 6;

                            string errorTitle = "API version not supported";
                            string errorMsg = "Detected a Cloud Controller API version lower than the minimum supported version (3.63.0); some features of this extension may not work as expected for the given instance.";

                            _logger.Information(errorMsg);
                            _dialogService.DisplayErrorDialog(errorTitle, errorMsg);
                        }
                        else if (apiVersion < new Version("3.85.0"))
                        {
                            _fileService.CliVersion = 6;
                        }
                        else
                        {
                            _fileService.CliVersion = 7;
                        }

                        break;

                    default:
                        _fileService.CliVersion = 7;
                        _logger.Information("Detected an unexpected Cloud Controller API version: {apiVersion}. CLI version has been set to 7 by default.", apiVersion);

                        break;
                }
            }
        }
    }
}