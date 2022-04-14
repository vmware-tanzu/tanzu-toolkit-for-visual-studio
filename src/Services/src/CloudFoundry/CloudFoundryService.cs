using Microsoft.Extensions.DependencyInjection;
using Serilog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Security;
using System.Threading;
using System.Threading.Tasks;
using Tanzu.Toolkit.CloudFoundryApiClient;
using Tanzu.Toolkit.CloudFoundryApiClient.Models;
using Tanzu.Toolkit.CloudFoundryApiClient.Models.AppsResponse;
using Tanzu.Toolkit.CloudFoundryApiClient.Models.OrgsResponse;
using Tanzu.Toolkit.CloudFoundryApiClient.Models.SpacesResponse;
using Tanzu.Toolkit.CloudFoundryApiClient.Models.StacksResponse;
using Tanzu.Toolkit.Models;
using Tanzu.Toolkit.Services.CfCli;
using Tanzu.Toolkit.Services.ErrorDialog;
using Tanzu.Toolkit.Services.File;
using Tanzu.Toolkit.Services.Logging;

namespace Tanzu.Toolkit.Services.CloudFoundry
{
    public class CloudFoundryService : ICloudFoundryService
    {
        internal const string _emptyOutputDirMessage = "Unable to locate app files; project output directory is empty. (Has your project already been compiled?)";
        internal const string _ccApiVersionUndetectableErrTitle = "Unable to detect Cloud Controller API version.";
        internal const string _ccApiVersionUndetectableErrMsg = "Failed to detect which version of the Cloud Controller API is being run on the provided instance; some features of this extension may not work properly.";
        internal const string _loginFailureMessage = "Login failed.";
        internal const string _cfApiSsoPromptKey = "passcode";
        internal const string _routeDeletionErrorMsg = "Encountered error deleting certain routes";
        private readonly ICfApiClient _cfApiClient;
        private readonly ICfCliService _cfCliService;
        private readonly IFileService _fileService;
        private readonly IErrorDialog _dialogService;
        private readonly ILogger _logger;
        private readonly ISerializationService _serializationService;

        public CloudFoundryService(IServiceProvider services)
        {
            _cfApiClient = services.GetRequiredService<ICfApiClient>();
            _cfCliService = services.GetRequiredService<ICfCliService>();
            _fileService = services.GetRequiredService<IFileService>();
            _dialogService = services.GetRequiredService<IErrorDialog>();
            _serializationService = services.GetRequiredService<ISerializationService>();

            var logSvc = services.GetRequiredService<ILoggingService>();
            _logger = logSvc.Logger;
        }

        internal string CfApiAddress { get; set; }

        public DetailedResult ConfigureForCf(CloudFoundryInstance cf)
        {
            try
            {
                var uri = new Uri(cf.ApiAddress, UriKind.Absolute);
                _cfApiClient.Configure(uri, cf.SkipSslCertValidation);
                CfApiAddress = uri.ToString();
                return new DetailedResult { Succeeded = true };
            }
            catch (Exception ex)
            {
                return new DetailedResult
                {
                    Succeeded = false,
                    Explanation = ex.Message
                };
            }
        }

        public DetailedResult TargetCfApi(string targetApiAddress, bool skipSsl)
        {
            return _cfCliService.TargetApi(targetApiAddress, skipSsl);
        }

        /// <summary>
        /// Performs authentication with Cloud Foundry API via CF CLI `cf auth` command.
        /// Expects api address to already have been targeted
        /// </summary>
        /// <param name="username"></param>
        /// <param name="password"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        /// <exception cref="ArgumentNullException"></exception>
        public async Task<DetailedResult> LoginWithCredentials(string username, SecureString password)
        {
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
                var authResult = await _cfCliService.AuthenticateAsync(username, password);

                if (!authResult.Succeeded)
                {
                    if (authResult.FailureType != FailureType.InvalidCertificate)
                    {
                        authResult.Explanation = _loginFailureMessage + Environment.NewLine + $"Unable to authenticate user \"{username}\"" + Environment.NewLine + authResult.CmdResult.StdErr;
                    }

                    return authResult;
                }

                await MatchCliVersionToApiVersion();

                return new DetailedResult
                {
                    Succeeded = true,
                    Explanation = null
                };
            }
            catch (Exception e)
            {
                var errorMessages = new List<string>();
                FormatExceptionMessage(e, errorMessages);
                var errorMessage = string.Join(Environment.NewLine, errorMessages.ToArray());

                return new DetailedResult
                {
                    Succeeded = false,
                    Explanation = errorMessage
                };
            }
        }

        public async Task<DetailedResult<string>> GetSsoPrompt(string cfApiAddress, bool skipSsl = false)
        {
            try
            {
                var loginServerInfo = await _cfApiClient.GetLoginServerInformation(cfApiAddress, skipSsl);

                if (loginServerInfo.Prompts.ContainsKey(_cfApiSsoPromptKey))
                {
                    var ssoPasscodePrompt = loginServerInfo.Prompts[_cfApiSsoPromptKey][1];

                    return new DetailedResult<string>
                    {
                        Succeeded = true,
                        Content = ssoPasscodePrompt,
                    };
                }

                return new DetailedResult<string>
                {
                    Succeeded = false,
                    Explanation = "Unable to determine SSO URL.",
                    FailureType = FailureType.MissingSsoPrompt,
                };
            }
            catch (Exception ex)
            {
                return new DetailedResult<string>
                {
                    Succeeded = false,
                    Explanation = ex.Message,
                };
            }
        }

        public async Task<DetailedResult> LoginWithSsoPasscode(string cfApiAddress, string passcode)
        {
            return await _cfCliService.LoginWithSsoPasscode(cfApiAddress, passcode);
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
        public async Task<DetailedResult<List<CloudFoundryOrganization>>> GetOrgsForCfInstanceAsync(CloudFoundryInstance cf, bool skipSsl = false, int retryAmount = 1)
        {
            var apiAddress = cf.ApiAddress;

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
                    _logger.Error("GetOrgsForCfInstanceAsync encountered exception: {GetOrgsForCfInstanceAsyncException}", originalException);

                    return new DetailedResult<List<CloudFoundryOrganization>>()
                    {
                        Succeeded = false,
                        Explanation = originalException.Message,
                    };
                }
            }

            var orgsToReturn = new List<CloudFoundryOrganization>();
            foreach (var org in orgsFromApi)
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
        public async Task<DetailedResult<List<CloudFoundrySpace>>> GetSpacesForOrgAsync(CloudFoundryOrganization org, bool skipSsl = false, int retryAmount = 1)
        {
            var apiAddress = org.ParentCf.ApiAddress;

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
                    _logger.Error("GetSpacesForOrgAsync encountered exception: {GetSpacesForOrgAsyncException}", originalException);

                    return new DetailedResult<List<CloudFoundrySpace>>()
                    {
                        Succeeded = false,
                        Explanation = originalException.Message,
                    };
                }
            }

            var spacesToReturn = new List<CloudFoundrySpace>();
            foreach (var space in spacesFromApi)
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
        public async Task<DetailedResult<List<CloudFoundryApp>>> GetAppsForSpaceAsync(CloudFoundrySpace space, bool skipSsl = false, int retryAmount = 1)
        {
            var apiAddress = space.ParentOrg.ParentCf.ApiAddress;

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
                    _logger.Error("GetSpacesForOrgAsync encountered exception: {GetSpacesForOrgAsyncException}", originalException);

                    return new DetailedResult<List<CloudFoundryApp>>()
                    {
                        Succeeded = false,
                        Explanation = originalException.Message,
                    };
                }
            }

            var appsToReturn = new List<CloudFoundryApp>();
            foreach (var app in appsFromApi)
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
                    appsToReturn.Add(new CloudFoundryApp(app.Name, app.Guid, space, app.State.ToUpper())
                    {
                        Stack = app.Lifecycle.Data.Stack,
                        Buildpacks = new List<string>(app.Lifecycle.Data.Buildpacks),
                    });
                }
            }

            return new DetailedResult<List<CloudFoundryApp>>()
            {
                Succeeded = true,
                Content = appsToReturn,
            };
        }

        public async Task<DetailedResult<List<CloudFoundryApp>>> ListAllAppsAsync(int retryAmount = 1)
        {
            string accessToken;
            try
            {
                accessToken = _cfCliService.GetOAuthToken();
            }
            catch (InvalidRefreshTokenException)
            {
                var msg = "Unable to retrieve apps because the connection has expired. Please log back in to re-authenticate.";
                _logger.Information(msg);

                return new DetailedResult<List<CloudFoundryApp>>
                {
                    Succeeded = false,
                    Explanation = msg,
                    Content = null,
                    FailureType = FailureType.InvalidRefreshToken,
                };
            }

            if (accessToken == null)
            {
                const string msg = "CloudFoundryService attempted to list apps but was unable to look up an access token.";
                _logger.Error(msg);

                return new DetailedResult<List<CloudFoundryApp>>()
                {
                    Succeeded = false,
                    Explanation = msg,
                };
            }

            List<App> appsFromApi;
            try
            {
                appsFromApi = await _cfApiClient.ListAppsAsync(accessToken);
            }
            catch (Exception originalException)
            {
                if (retryAmount > 0)
                {
                    _logger.Information("ListAllAppsAsync caught an exception when trying to retrieve apps: {originalException}. About to clear the cached access token & try again ({retryAmount} retry attempts remaining).", originalException.Message, retryAmount);
                    _cfCliService.ClearCachedAccessToken();
                    retryAmount -= 1;
                    return await ListAllAppsAsync(retryAmount);
                }
                else
                {
                    _logger.Error("GetSpacesForOrgAsync encountered exception: {GetSpacesForOrgAsyncException}", originalException);

                    return new DetailedResult<List<CloudFoundryApp>>()
                    {
                        Succeeded = false,
                        Explanation = originalException.Message,
                    };
                }
            }

            var appsToReturn = new List<CloudFoundryApp>();
            foreach (var app in appsFromApi)
            {
                if (app.Name == null)
                {
                    _logger.Error("CloudFoundryService.ListAllAppsAsync encountered an app without a name; omitting it from the returned list of apps.");
                }
                else if (app.Guid == null)
                {
                    _logger.Error("CloudFoundryService.ListAllAppsAsync encountered an app without a guid; omitting it from the returned list of apps.");
                }
                else
                {
                    appsToReturn.Add(new CloudFoundryApp(app.Name, app.Guid, null, app.State.ToUpper()));
                }
            }

            return new DetailedResult<List<CloudFoundryApp>>()
            {
                Succeeded = true,
                Content = appsToReturn,
            };
        }

        public async Task<DetailedResult<List<CfBuildpack>>> GetBuildpacksAsync(string apiAddress, int retryAmount = 1)
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

                return new DetailedResult<List<CfBuildpack>>
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

                return new DetailedResult<List<CfBuildpack>>()
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
                    _logger.Information("GetBuildpacksAsync caught an exception when trying to retrieve buildpacks: {originalException}. About to clear the cached access token & try again ({retryAmount} retry attempts remaining).", originalException.Message, retryAmount);
                    _cfCliService.ClearCachedAccessToken();
                    retryAmount -= 1;
                    return await GetBuildpacksAsync(apiAddress, retryAmount);
                }
                else
                {
                    _logger.Error("GetSpacesForOrgAsync encountered exception: {GetSpacesForOrgAsyncException}", originalException);

                    return new DetailedResult<List<CfBuildpack>>()
                    {
                        Succeeded = false,
                        Explanation = originalException.Message,
                    };
                }
            }

            var buildpacks = new List<CfBuildpack>();

            foreach (var buildpack in buildpacksFromApi)
            {
                if (string.IsNullOrWhiteSpace(buildpack.Name))
                {
                    _logger.Error("CloudFoundryService.GetBuildpacksAsync encountered a buildpack without a name; omitting it from the returned list of buildpacks.");
                }
                if (string.IsNullOrWhiteSpace(buildpack.Stack))
                {
                    _logger.Error("CloudFoundryService.GetBuildpacksAsync encountered a buildpack without a stack; omitting it from the returned list of buildpacks.");
                }
                else
                {
                    buildpacks.Add(new CfBuildpack { Name = buildpack.Name, Stack = buildpack.Stack });
                }
            }

            return new DetailedResult<List<CfBuildpack>>()
            {
                Succeeded = true,
                Content = buildpacks,
            };
        }

        public async Task<DetailedResult<List<CfService>>> GetServicesAsync(string apiAddress, int retryAmount = 1)
        {
            string accessToken;
            try
            {
                accessToken = _cfCliService.GetOAuthToken();
            }
            catch (InvalidRefreshTokenException)
            {
                var msg = "Unable to retrieve services because the connection has expired. Please log back in to re-authenticate.";
                _logger.Information(msg);

                return new DetailedResult<List<CfService>>
                {
                    Succeeded = false,
                    Explanation = msg,
                    Content = null,
                    FailureType = FailureType.InvalidRefreshToken,
                };
            }

            if (accessToken == null)
            {
                _logger.Error("CloudFoundryService attempted to get services but was unable to look up an access token.");

                return new DetailedResult<List<CfService>>()
                {
                    Succeeded = false,
                    Explanation = $"CloudFoundryService attempted to get services but was unable to look up an access token.",
                };
            }

            List<Service> servicesFromApi;
            try
            {
                servicesFromApi = await _cfApiClient.ListServices(apiAddress, accessToken);
            }
            catch (Exception originalException)
            {
                if (retryAmount > 0)
                {
                    _logger.Information("GetServicesAsync caught an exception when trying to retrieve services: {originalException}. About to clear the cached access token & try again ({retryAmount} retry attempts remaining).", originalException.Message, retryAmount);
                    _cfCliService.ClearCachedAccessToken();
                    retryAmount -= 1;
                    return await GetServicesAsync(apiAddress, retryAmount);
                }
                else
                {
                    _logger.Error("GetServicesAsync encountered exception: {GetServicesAsyncException}", originalException);

                    return new DetailedResult<List<CfService>>()
                    {
                        Succeeded = false,
                        Explanation = originalException.Message,
                    };
                }
            }

            var services = new List<CfService>();

            foreach (var service in servicesFromApi)
            {
                if (string.IsNullOrWhiteSpace(service.Name))
                {
                    _logger.Error("CloudFoundryService.GetServicesAsync encountered a service without a name; omitting it from the returned list of services.");
                }
                else
                {
                    services.Add(new CfService { Name = service.Name });
                }
            }

            return new DetailedResult<List<CfService>>()
            {
                Succeeded = true,
                Content = services,
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
        public async Task<DetailedResult> StopAppAsync(CloudFoundryApp app, bool skipSsl = false, int retryAmount = 1)
        {
            var apiAddress = app.ParentSpace.ParentOrg.ParentCf.ApiAddress;

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
                    _logger.Error("StopAppAsync encountered exception: {StopAppAsyncException}", originalException);

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
        public async Task<DetailedResult> StartAppAsync(CloudFoundryApp app, bool skipSsl = false, int retryAmount = 1)
        {
            var apiAddress = app.ParentSpace.ParentOrg.ParentCf.ApiAddress;

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
                    _logger.Error("StartAppAsync encountered exception: {StartAppAsyncException}", originalException);

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
        public async Task<DetailedResult> DeleteAppAsync(CloudFoundryApp app, bool skipSsl = false, bool removeRoutes = false, int retryAmount = 1)
        {
            var apiAddress = app.ParentSpace.ParentOrg.ParentCf.ApiAddress;

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
                if (removeRoutes)
                {
                    var routeDeletionResult = await DeleteAllRoutesForAppAsync(app);
                    if (!routeDeletionResult.Succeeded)
                    {
                        return new DetailedResult
                        {
                            Succeeded = false,
                            Explanation = $"{routeDeletionResult.Explanation}. Please try deleting '{app.AppName}' again",
                        };
                    }
                }

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
                    _logger.Error("StartAppAsync encountered exception: {StartAppAsyncException}", originalException);

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

        public async Task<DetailedResult<List<CloudFoundryRoute>>> GetRoutesForAppAsync(CloudFoundryApp app, int retryAmount = 1)
        {
            var apiAddress = app.ParentSpace.ParentOrg.ParentCf.ApiAddress;

            string accessToken;
            try
            {
                accessToken = _cfCliService.GetOAuthToken();
            }
            catch (InvalidRefreshTokenException)
            {
                var msg = "Unable to retrieve routes for '{AppName}' because the connection to '{CfName}' has expired. Please log back in to re-authenticate.";
                _logger.Information(msg, app.AppName, app.ParentSpace.ParentOrg.ParentCf.InstanceName);

                return new DetailedResult<List<CloudFoundryRoute>>()
                {
                    Succeeded = false,
                    Explanation = msg.Replace("{AppName}", app.AppName).Replace("CfName", app.ParentSpace.ParentOrg.ParentCf.InstanceName),
                    FailureType = FailureType.InvalidRefreshToken,
                };
            }

            if (accessToken == null)
            {
                _logger.Error("CloudFoundryService attempted to get routes for '{appName}' but was unable to look up an access token.", app.AppName);

                return new DetailedResult<List<CloudFoundryRoute>>()
                {
                    Succeeded = false,
                    Explanation = $"CloudFoundryService attempted to get routes for '{app.AppName}' but was unable to look up an access token.",
                };
            }

            List<Route> routesFromApi;
            try
            {
                routesFromApi = await _cfApiClient.ListRoutesForApp(apiAddress, accessToken, app.AppId);
            }
            catch (Exception originalException)
            {
                if (retryAmount > 0)
                {
                    _logger.Information("GetRoutesForAppAsync caught an exception when trying to retrieve routes: {originalException}. About to clear the cached access token & try again ({retryAmount} retry attempts remaining).", originalException.Message, retryAmount);
                    _cfCliService.ClearCachedAccessToken();
                    retryAmount -= 1;
                    return await GetRoutesForAppAsync(app, retryAmount);
                }
                else
                {
                    _logger.Error("GetRoutesForAppAsync encountered exception: {GetRoutesForAppAsyncException}", originalException);

                    return new DetailedResult<List<CloudFoundryRoute>>()
                    {
                        Succeeded = false,
                        Explanation = originalException.Message,
                    };
                }
            }

            var routes = new List<CloudFoundryRoute>();

            foreach (var route in routesFromApi)
            {
                if (string.IsNullOrWhiteSpace(route.Guid))
                {
                    _logger.Error("CloudFoundryService.GetRoutesForAppAsync encountered a route without a guid; omitting it from the returned list of routes.");
                }
                else
                {
                    routes.Add(new CloudFoundryRoute(route.Guid));
                }
            }

            return new DetailedResult<List<CloudFoundryRoute>>
            {
                Succeeded = true,
                Content = routes,
            };

        }

        public async Task<DetailedResult> DeleteAllRoutesForAppAsync(CloudFoundryApp app)
        {
            var apiAddress = app.ParentSpace.ParentOrg.ParentCf.ApiAddress;

            string accessToken;
            try
            {
                accessToken = _cfCliService.GetOAuthToken();
            }
            catch (InvalidRefreshTokenException)
            {
                var msg = "Unable to retrieve routes for '{AppName}' because the connection to '{CfName}' has expired. Please log back in to re-authenticate.";
                _logger.Information(msg, app.AppName, app.ParentSpace.ParentOrg.ParentCf.InstanceName);

                return new DetailedResult()
                {
                    Succeeded = false,
                    Explanation = msg.Replace("{AppName}", app.AppName).Replace("CfName", app.ParentSpace.ParentOrg.ParentCf.InstanceName),
                    FailureType = FailureType.InvalidRefreshToken,
                };
            }

            if (accessToken == null)
            {
                _logger.Error("CloudFoundryService attempted to get routes for '{appName}' but was unable to look up an access token.", app.AppName);

                return new DetailedResult()
                {
                    Succeeded = false,
                    Explanation = $"CloudFoundryService attempted to get routes for '{app.AppName}' but was unable to look up an access token.",
                };
            }

            var routesResponse = await GetRoutesForAppAsync(app); // this has the potential to refresh an expired access token
            if (!routesResponse.Succeeded)
            {
                return routesResponse;
            }

            var routes = routesResponse.Content;
            var routeDeletionTasks = new List<Task>();
            var failed = 0;
            foreach (var route in routes)
            {
                routeDeletionTasks.Add(Task.Run(async () =>
                {
                    var routeWasDeleted = await _cfApiClient.DeleteRouteWithGuid(apiAddress, accessToken, route.RouteGuid);
                    if (!routeWasDeleted)
                    {
                        Interlocked.Increment(ref failed);
                    }
                }));
            }

            var allRouteDeletions = Task.WhenAll(routeDeletionTasks);

            try
            {
                allRouteDeletions.Wait();
            }
            catch (Exception ex)
            {
                _logger.Error("{RouteDeletionMessage}; {RouteDeletionException}", _routeDeletionErrorMsg, ex.Message);

                return new DetailedResult
                {
                    Succeeded = false,
                    Explanation = _routeDeletionErrorMsg,
                };
            }

            if (allRouteDeletions.Status != TaskStatus.RanToCompletion)
            {
                _logger.Error("Not all route deletion tasks ran to completion. {RouteDeletionMessage}", _routeDeletionErrorMsg);

                return new DetailedResult
                {
                    Succeeded = false,
                    Explanation = _routeDeletionErrorMsg,
                };
            }

            if (failed > 0)
            {
                _logger.Error("{RouteDeletionMessage}; {NumFailedRouteDeletions} routes were not deleted", _routeDeletionErrorMsg, failed);

                return new DetailedResult
                {
                    Succeeded = false,
                    Explanation = _routeDeletionErrorMsg,
                };
            }

            return new DetailedResult
            {
                Succeeded = true,
            };
        }

        public async Task<DetailedResult> DeployAppAsync(AppManifest appManifest, string defaultAppPath, CloudFoundryInstance targetCf, CloudFoundryOrganization targetOrg, CloudFoundrySpace targetSpace, Action<string> stdOutCallback, Action<string> stdErrCallback)
        {
            var app = appManifest.Applications[0];

            var pathToDeploymentDirectory = app.Path ?? defaultAppPath;
            var appName = app.Name;

            if (!_fileService.DirContainsFiles(pathToDeploymentDirectory))
            {
                return new DetailedResult(false, _emptyOutputDirMessage);
            }

            var newManifestPath = _fileService.GetUniquePathForTempFile($"temp_manifest_{appName}");
            var manifestCreationResult = CreateManifestFile(newManifestPath, appManifest);

            if (!manifestCreationResult.Succeeded)
            {
                _logger.Error("Unable to push app due to manifest creation failure: {ManifestCreationError}", manifestCreationResult.Explanation);
                return new DetailedResult(false, $"Manifest compilation failed while attempting to push app {appName}:\n{manifestCreationResult.Explanation}");
            }

            DetailedResult cfPushResult;
            try
            {
                cfPushResult = await _cfCliService.PushAppAsync(newManifestPath, pathToDeploymentDirectory, targetOrg.OrgName, targetSpace.SpaceName, stdOutCallback, stdErrCallback);
            }
            catch (InvalidRefreshTokenException)
            {
                var msg = "Unable to deploy app '{AppName}' to '{CfName}' because the connection has expired. Please log back in to re-authenticate.";
                _logger.Information(msg, appName, targetCf.InstanceName);

                _fileService.DeleteFile(newManifestPath);

                return new DetailedResult
                {
                    Succeeded = false,
                    Explanation = msg.Replace("{AppName}", appName).Replace("{CfName}", targetCf.InstanceName),
                    FailureType = FailureType.InvalidRefreshToken,
                };
            }

            if (!cfPushResult.Succeeded)
            {
                _fileService.DeleteFile(newManifestPath);

                _logger.Error("Successfully targeted org '{targetOrgName}' and space '{targetSpaceName}' but app deployment failed at the `cf push` stage.\n{cfPushResult}", targetOrg.OrgName, targetSpace.SpaceName, cfPushResult.Explanation);
                return new DetailedResult(false, cfPushResult.Explanation);
            }

            _fileService.DeleteFile(newManifestPath);

            return new DetailedResult(true, $"App successfully deploying to org '{targetOrg.OrgName}', space '{targetSpace.SpaceName}'...");
        }

        public async Task<DetailedResult<string>> GetRecentLogsAsync(CloudFoundryApp app)
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

        public DetailedResult<Process> StreamAppLogs(CloudFoundryApp app, Action<string> stdOutCallback, Action<string> stdErrCallback)
        {
            try
            {
                return _cfCliService.StreamAppLogs(app.AppName, app.ParentSpace.ParentOrg.OrgName, app.ParentSpace.SpaceName, stdOutCallback, stdErrCallback);
            }
            catch (InvalidRefreshTokenException)
            {
                var msg = "Unable to stream app logs from '{AppName}' because the connection has expired. Please log back in to re-authenticate.";
                _logger.Information(msg, app.AppName);

                return new DetailedResult<Process>
                {
                    Succeeded = false,
                    Explanation = msg.Replace("{AppName}", app.AppName),
                    Content = null,
                    FailureType = FailureType.InvalidRefreshToken,
                };
            }
            catch (Exception ex)
            {
                return new DetailedResult<Process>
                {
                    Succeeded = false,
                    Explanation = ex.Message,
                };
            }
        }

        public DetailedResult CreateManifestFile(string location, AppManifest manifest)
        {
            try
            {
                var ymlContents = _serializationService.SerializeCfAppManifest(manifest);

                _fileService.WriteTextToFile(location, "---\n" + ymlContents);

                return new DetailedResult
                {
                    Succeeded = true,
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

        public async Task<DetailedResult<List<string>>> GetStackNamesAsync(CloudFoundryInstance cf, int retryAmount = 1)
        {
            var apiAddress = cf.ApiAddress;

            string accessToken;
            try
            {
                accessToken = _cfCliService.GetOAuthToken();
            }
            catch (InvalidRefreshTokenException)
            {
                var msg = "Unable to retrieve stacks for '{CfName}' because the connection has expired. Please log back in to re-authenticate.";
                _logger.Information(msg, cf.InstanceName);

                return new DetailedResult<List<string>>
                {
                    Succeeded = false,
                    Explanation = msg.Replace("{CfName}", cf.InstanceName),
                    Content = null,
                    FailureType = FailureType.InvalidRefreshToken,
                };
            }

            if (accessToken == null)
            {
                _logger.Error("CloudFoundryService attempted to get stacks for {apiAddress} but was unable to look up an access token.", apiAddress);

                return new DetailedResult<List<string>>()
                {
                    Succeeded = false,
                    Explanation = $"CloudFoundryService attempted to get stacks for '{apiAddress}' but was unable to look up an access token.",
                };
            }

            List<Stack> stacksFromApi;
            try
            {
                stacksFromApi = await _cfApiClient.ListStacks(apiAddress, accessToken);
            }
            catch (Exception originalException)
            {
                if (retryAmount > 0)
                {
                    _logger.Information("GetStackNamesAsync caught an exception when trying to retrieve stacks: {originalException}. About to clear the cached access token & try again ({retryAmount} retry attempts remaining).", originalException.Message, retryAmount);
                    _cfCliService.ClearCachedAccessToken();
                    retryAmount -= 1;
                    return await GetStackNamesAsync(cf, retryAmount);
                }
                else
                {
                    _logger.Error("GetStackNamesAsync encountered exception: {GetStackNamesAsyncException}", originalException);

                    return new DetailedResult<List<string>>()
                    {
                        Succeeded = false,
                        Explanation = originalException.Message,
                    };
                }
            }

            var stackNamesToReturn = new List<string>();
            foreach (var stack in stacksFromApi)
            {
                if (stack.Name == null)
                {
                    _logger.Error("CloudFoundryService.GetStackNamesAsync encountered a stack without a name; omitting it from the returned list of stacks.");
                }
                else
                {
                    stackNamesToReturn.Add(stack.Name);
                }
            }

            return new DetailedResult<List<string>>()
            {
                Succeeded = true,
                Content = stackNamesToReturn,
            };
        }

        public void LogoutCfUser()
        {
            _cfCliService.Logout();
            _cfCliService.ClearCachedAccessToken();
        }

        private void FormatExceptionMessage(Exception ex, List<string> message)
        {
            if (ex is AggregateException aex)
            {
                foreach (var iex in aex.InnerExceptions)
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
            var apiVersion = await _cfCliService.GetApiVersion();
            if (apiVersion == null)
            {
                _fileService.CliVersion = 7;
                _dialogService.DisplayErrorDialog(_ccApiVersionUndetectableErrTitle, _ccApiVersionUndetectableErrMsg);
            }
            else
            {
                switch (apiVersion.Major)
                {
                    case 2:
                        if (apiVersion < new Version("2.128.0"))
                        {
                            _fileService.CliVersion = 6;

                            var errorTitle = "API version not supported";
                            var errorMsg = "Detected a Cloud Controller API version lower than the minimum supported version (2.128.0); some features of this extension may not work as expected for the given instance.";

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

                            var errorTitle = "API version not supported";
                            var errorMsg = "Detected a Cloud Controller API version lower than the minimum supported version (3.63.0); some features of this extension may not work as expected for the given instance.";

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