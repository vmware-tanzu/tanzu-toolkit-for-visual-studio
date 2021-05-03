using Microsoft.Extensions.DependencyInjection;
using Serilog;
using System;
using System.Collections.Generic;
using System.Security;
using System.Threading.Tasks;
using Tanzu.Toolkit.CloudFoundryApiClient;
using Tanzu.Toolkit.CloudFoundryApiClient.Models.AppsResponse;
using Tanzu.Toolkit.CloudFoundryApiClient.Models.OrgsResponse;
using Tanzu.Toolkit.CloudFoundryApiClient.Models.SpacesResponse;
using Tanzu.Toolkit.VisualStudio.Models;
using Tanzu.Toolkit.VisualStudio.Services.CfCli;
using Tanzu.Toolkit.VisualStudio.Services.Dialog;
using Tanzu.Toolkit.VisualStudio.Services.FileLocator;
using Tanzu.Toolkit.VisualStudio.Services.Logging;
using static Tanzu.Toolkit.VisualStudio.Services.OutputHandler.OutputHandler;

namespace Tanzu.Toolkit.VisualStudio.Services.CloudFoundry
{
    public class CloudFoundryService : ICloudFoundryService
    {
        private static ICfApiClient cfApiClient;
        private static ICfCliService cfCliService;
        private static IFileLocatorService _fileLocatorService;
        private static IDialogService dialogService;
        private static ILogger logger;

        internal const string emptyOutputDirMessage = "Unable to locate app files; project output directory is empty. (Has your project already been compiled?)";
        internal const string ccApiVersionUndetectableErrTitle = "Unable to detect Cloud Controller API version.";
        internal const string ccApiVersionUndetectableErrMsg = "Failed to detect which version of the Cloud Controller API is being run on the provided instance; some features of this extension may not work properly.";

        public string LoginFailureMessage { get; } = "Login failed.";
        public Dictionary<string, CloudFoundryInstance> CloudFoundryInstances { get; internal set; }
        public CloudFoundryInstance ActiveCloud { get; set; }

        public CloudFoundryService(IServiceProvider services)
        {
            CloudFoundryInstances = new Dictionary<string, CloudFoundryInstance>();

            cfApiClient = services.GetRequiredService<ICfApiClient>();
            cfCliService = services.GetRequiredService<ICfCliService>();
            _fileLocatorService = services.GetRequiredService<IFileLocatorService>();
            dialogService = services.GetRequiredService<IDialogService>();

            var logSvc = services.GetRequiredService<ILoggingService>();
            logger = logSvc.Logger;
        }

        public void AddCloudFoundryInstance(string name, string apiAddress, string accessToken)
        {
            if (CloudFoundryInstances.ContainsKey(name)) throw new Exception($"The name {name} already exists.");
            CloudFoundryInstances.Add(name, new CloudFoundryInstance(name, apiAddress, accessToken));
        }

        public void RemoveCloudFoundryInstance(string name)
        {
            if (CloudFoundryInstances.ContainsKey(name)) CloudFoundryInstances.Remove(name);
        }

        public async Task<ConnectResult> ConnectToCFAsync(string targetApiAddress, string username, SecureString password, string httpProxy, bool skipSsl)
        {
            if (string.IsNullOrEmpty(targetApiAddress)) throw new ArgumentException(nameof(targetApiAddress));

            if (string.IsNullOrEmpty(username)) throw new ArgumentException(nameof(username));

            if (password == null) throw new ArgumentNullException(nameof(password));

            try
            {
                DetailedResult targetResult = cfCliService.TargetApi(targetApiAddress, skipSsl);
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

                DetailedResult authResult = await cfCliService.AuthenticateAsync(username, password);
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

                string accessToken = cfCliService.GetOAuthToken();

                if (!string.IsNullOrEmpty(accessToken)) return new ConnectResult(true, null, accessToken);

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

        private static async Task MatchCliVersionToApiVersion()
        {
            Version apiVersion = await cfCliService.GetApiVersion();
            if (apiVersion == null)
            {
                _fileLocatorService.CliVersion = 7;
                dialogService.DisplayErrorDialog(ccApiVersionUndetectableErrTitle, ccApiVersionUndetectableErrMsg);
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

                            logger.Information(errorMsg);
                            dialogService.DisplayErrorDialog(errorTitle, errorMsg);
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

                            logger.Information(errorMsg);
                            dialogService.DisplayErrorDialog(errorTitle, errorMsg);
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
                        logger.Information($"Detected an unexpected Cloud Controller API version: {apiVersion}. CLI version has been set to 7 by default.");

                        break;
                }
            }
        }

        /// <summary>
        /// Requests orgs from <see cref="CfApiClient"/> using access token from <see cref="CfCliService"/>.
        /// </summary>
        /// <param name="cf"></param>
        /// <param name="skipSsl"></param>
        public async Task<DetailedResult<List<CloudFoundryOrganization>>> GetOrgsForCfInstanceAsync(CloudFoundryInstance cf, bool skipSsl = true)
        {
            List<Org> orgsFromApi;
            var orgsToReturn = new List<CloudFoundryOrganization>();

            string apiAddress = cf.ApiAddress;

            var accessToken = cfCliService.GetOAuthToken();
            if (accessToken == null)
            {
                var msg = $"CloudFoundryService attempted to get orgs for '{apiAddress}' but was unable to look up an access token.";
                logger.Error(msg);

                return new DetailedResult<List<CloudFoundryOrganization>>()
                {
                    Succeeded = false,
                    Explanation = msg,
                };
            }

            try
            {
                orgsFromApi = await cfApiClient.ListOrgs(apiAddress, accessToken);
            }
            catch (Exception originalException)
            {
                var msg = $"Something went wrong while trying to request orgs from {apiAddress}: {originalException.Message}";
                logger.Error(msg);

                return new DetailedResult<List<CloudFoundryOrganization>>()
                {
                    Succeeded = false,
                    Explanation = msg,
                };
            }

            foreach (Org org in orgsFromApi)
            {
                if (org.Name == null)
                {
                    logger.Error("CloudFoundryService.GetOrgsForCfInstanceAsync encountered an org without a name; omitting it from the returned list of orgs.");
                }
                else if (org.Guid == null)
                {
                    logger.Error("CloudFoundryService.GetOrgsForCfInstanceAsync encountered an org without a guid; omitting it from the returned list of orgs.");
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
        /// </summary>
        /// <param name="org"></param>
        /// <param name="skipSsl"></param>
        public async Task<DetailedResult<List<CloudFoundrySpace>>> GetSpacesForOrgAsync(CloudFoundryOrganization org, bool skipSsl = true)
        {
            List<Space> spacesFromApi;
            var spacesToReturn = new List<CloudFoundrySpace>();

            string apiAddress = org.ParentCf.ApiAddress;

            var accessToken = cfCliService.GetOAuthToken();
            if (accessToken == null)
            {
                var msg = $"CloudFoundryService attempted to get spaces for '{org.OrgName}' but was unable to look up an access token.";
                logger.Error(msg);

                return new DetailedResult<List<CloudFoundrySpace>>()
                {
                    Succeeded = false,
                    Explanation = msg,
                };
            }

            try
            {
                spacesFromApi = await cfApiClient.ListSpacesForOrg(apiAddress, accessToken, org.OrgId);
            }
            catch (Exception originalException)
            {
                var msg = $"Something went wrong while trying to request spaces from {apiAddress}: {originalException.Message}";
                logger.Error(msg);

                return new DetailedResult<List<CloudFoundrySpace>>()
                {
                    Succeeded = false,
                    Explanation = msg,
                };
            }

            foreach (Space space in spacesFromApi)
            {
                if (space.Name == null)
                {
                    logger.Error("CloudFoundryService.GetSpacesForOrgAsync encountered a space without a name; omitting it from the returned list of spaces.");
                }
                else if (space.Guid == null)
                {
                    logger.Error("CloudFoundryService.GetSpacesForOrgAsync encountered a space without a guid; omitting it from the returned list of spaces.");
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
        /// </summary>
        /// <param name="space"></param>
        /// <param name="skipSsl"></param>
        public async Task<DetailedResult<List<CloudFoundryApp>>> GetAppsForSpaceAsync(CloudFoundrySpace space, bool skipSsl = true)
        {
            List<App> appsFromApi;
            var appsToReturn = new List<CloudFoundryApp>();

            string apiAddress = space.ParentOrg.ParentCf.ApiAddress;

            var accessToken = cfCliService.GetOAuthToken();
            if (accessToken == null)
            {
                var msg = $"CloudFoundryService attempted to get apps for '{space.SpaceName}' but was unable to look up an access token.";
                logger.Error(msg);

                return new DetailedResult<List<CloudFoundryApp>>()
                {
                    Succeeded = false,
                    Explanation = msg,
                };
            }

            try
            {
                appsFromApi = await cfApiClient.ListAppsForSpace(apiAddress, accessToken, space.SpaceId);
            }
            catch (Exception originalException)
            {
                var msg = $"Something went wrong while trying to request apps from {apiAddress}: {originalException.Message}";
                logger.Error(msg);

                return new DetailedResult<List<CloudFoundryApp>>()
                {
                    Succeeded = false,
                    Explanation = msg,
                };
            }

            foreach (App app in appsFromApi)
            {
                if (app.Name == null)
                {
                    logger.Error("CloudFoundryService.GetAppsForSpaceAsync encountered an app without a name; omitting it from the returned list of apps.");
                }
                else if (app.Guid == null)
                {
                    logger.Error("CloudFoundryService.GetAppsForSpaceAsync encountered an app without a guid; omitting it from the returned list of apps.");
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

        public static void FormatExceptionMessage(Exception ex, List<string> message)
        {
            var aex = ex as AggregateException;
            if (aex != null)
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

        /// <summary>
        /// Stop <paramref name="app"/> using token from <see cref="CfCliService"/>.
        /// </summary>
        /// <param name="app"></param>
        /// <param name="skipSsl"></param>
        public async Task<DetailedResult> StopAppAsync(CloudFoundryApp app, bool skipSsl = true)
        {
            bool appWasStopped;

            string apiAddress = app.ParentSpace.ParentOrg.ParentCf.ApiAddress;

            var accessToken = cfCliService.GetOAuthToken();
            if (accessToken == null)
            {
                var msg = $"CloudFoundryService attempted to stop app '{app.AppName}' but was unable to look up an access token.";
                logger.Error(msg);

                return new DetailedResult
                {
                    Succeeded = false,
                    Explanation = msg,
                };
            }

            try
            {
                appWasStopped = await cfApiClient.StopAppWithGuid(apiAddress, accessToken, app.AppId);
            }
            catch (Exception originalException)
            {
                var msg = $"Something went wrong while trying to stop app '{app.AppName}': {originalException.Message}.";

                logger.Error(msg);

                return new DetailedResult
                {
                    Succeeded = false,
                    Explanation = msg,
                };
            }

            if (!appWasStopped)
            {
                var msg = $"Attempted to stop app '{app.AppName}' but it hasn't been stopped.";

                logger.Error(msg);

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
        /// </summary>
        /// <param name="app"></param>
        /// <param name="skipSsl"></param>
        public async Task<DetailedResult> StartAppAsync(CloudFoundryApp app, bool skipSsl = true)
        {
            bool appWasStarted;

            string apiAddress = app.ParentSpace.ParentOrg.ParentCf.ApiAddress;

            var accessToken = cfCliService.GetOAuthToken();
            if (accessToken == null)
            {
                var msg = $"CloudFoundryService attempted to start app '{app.AppName}' but was unable to look up an access token.";
                logger.Error(msg);

                return new DetailedResult
                {
                    Succeeded = false,
                    Explanation = msg,
                };
            }

            try
            {
                appWasStarted = await cfApiClient.StartAppWithGuid(apiAddress, accessToken, app.AppId);
            }
            catch (Exception originalException)
            {
                var msg = $"Something went wrong while trying to start app '{app.AppName}': {originalException.Message}.";

                logger.Error(msg);

                return new DetailedResult
                {
                    Succeeded = false,
                    Explanation = msg,
                };
            }

            if (!appWasStarted)
            {
                var msg = $"Attempted to start app '{app.AppName}' but it hasn't been started.";

                logger.Error(msg);

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

        public async Task<DetailedResult> DeleteAppAsync(CloudFoundryApp app, bool skipSsl = true, bool removeRoutes = true)
        {
            bool appWasDeleted;

            string apiAddress = app.ParentSpace.ParentOrg.ParentCf.ApiAddress;

            var accessToken = cfCliService.GetOAuthToken();
            if (accessToken == null)
            {
                var msg = $"CloudFoundryService attempted to delete app '{app.AppName}' but was unable to look up an access token.";
                logger.Error(msg);

                return new DetailedResult
                {
                    Succeeded = false,
                    Explanation = msg,
                };
            }

            try
            {
                appWasDeleted = await cfApiClient.DeleteAppWithGuid(apiAddress, accessToken, app.AppId);

                if (!appWasDeleted)
                {
                    var msg = $"Attempted to delete app '{app.AppName}' but it hasn't been deleted.";

                    logger.Error(msg);

                    return new DetailedResult
                    {
                        Succeeded = false,
                        Explanation = msg,
                    };
                }
            }
            catch (Exception originalException)
            {
                var msg = $"Something went wrong while trying to delete app '{app.AppName}': {originalException.Message}.";

                logger.Error(msg);

                return new DetailedResult
                {
                    Succeeded = false,
                    Explanation = msg,
                };
            }


            app.State = "DELETED";
            return new DetailedResult
            {
                Succeeded = true,
            };
        }

        public async Task<DetailedResult> DeployAppAsync(CloudFoundryInstance targetCf, CloudFoundryOrganization targetOrg, CloudFoundrySpace targetSpace, string appName, string appProjPath, StdOutDelegate stdOutCallback, StdErrDelegate stdErrCallback)
        {
            if (!_fileLocatorService.DirContainsFiles(appProjPath)) return new DetailedResult(false, emptyOutputDirMessage);

            DetailedResult cfTargetResult = await cfCliService.InvokeCfCliAsync(arguments: $"target -o {targetOrg.OrgName} -s {targetSpace.SpaceName}", stdOutCallback, stdErrCallback);
            if (!cfTargetResult.Succeeded) return new DetailedResult(false, $"Unable to target org '{targetOrg.OrgName}' or space '{targetSpace.SpaceName}'.\n{cfTargetResult.Explanation}");

            DetailedResult cfPushResult = await cfCliService.PushAppAsync(appName, stdOutCallback, stdErrCallback, appProjPath);
            if (!cfPushResult.Succeeded) return new DetailedResult(false, $"Successfully targeted org '{targetOrg.OrgName}' and space '{targetSpace.SpaceName}' but app deployment failed at the `cf push` stage.\n{cfPushResult.Explanation}");

            return new DetailedResult(true, $"App successfully deploying to org '{targetOrg.OrgName}', space '{targetSpace.SpaceName}'...");
        }

        public async Task<DetailedResult<string>> GetRecentLogs(CloudFoundryApp app)
        {
            /* CAUTION: these operations should happen atomically; 
             * if different threads all try to target orgs/spaces at the same time there's a risk of
             * getting unexpected results due to a race conditions for these values in .cf/config.json: 
             *      - "Target" (api address)
             *      - "OrganizationFields" (info about the currently-targeted org)
             *      - "SpaceFields" (info about the currently-targeted space)
             */


            var targetOrgResult = cfCliService.TargetOrg(app.ParentSpace.ParentOrg.OrgName);
            if (!targetOrgResult.Succeeded) return new DetailedResult<string>(
                content: null, 
                succeeded: false, 
                targetOrgResult.Explanation, 
                targetOrgResult.CmdDetails
            );

            var targetSpaceResult = cfCliService.TargetSpace(app.ParentSpace.SpaceName);
            if (!targetSpaceResult.Succeeded) return new DetailedResult<string>(
                content: null,
                succeeded: false,
                targetSpaceResult.Explanation,
                targetSpaceResult.CmdDetails
            );

            return await cfCliService.GetRecentAppLogs(app.AppName);
        }
    }
}