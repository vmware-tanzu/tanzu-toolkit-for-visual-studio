using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Security;
using System.Threading.Tasks;
using Tanzu.Toolkit.CloudFoundryApiClient;
using Tanzu.Toolkit.CloudFoundryApiClient.Models.AppsResponse;
using Tanzu.Toolkit.CloudFoundryApiClient.Models.SpacesResponse;
using Tanzu.Toolkit.VisualStudio.Models;
using Tanzu.Toolkit.VisualStudio.Services.CfCli;
using Tanzu.Toolkit.VisualStudio.Services.CfCli.Models;
using Tanzu.Toolkit.VisualStudio.Services.FileLocator;
using static Tanzu.Toolkit.VisualStudio.Services.OutputHandler.OutputHandler;

namespace Tanzu.Toolkit.VisualStudio.Services.CloudFoundry
{
    public class CloudFoundryService : ICloudFoundryService
    {
        private static ICfApiClient _cfApiClient;
        private static ICfCliService cfCliService;
        private static IFileLocatorService _fileLocatorService;
        internal const string emptyOutputDirMessage = "Unable to locate app files; project output directory is empty. (Has your project already been compiled?)";

        public string LoginFailureMessage { get; } = "Login failed.";
        public Dictionary<string, CloudFoundryInstance> CloudFoundryInstances { get; private set; }
        public CloudFoundryInstance ActiveCloud { get; set; }

        public CloudFoundryService(IServiceProvider services)
        {
            CloudFoundryInstances = new Dictionary<string, CloudFoundryInstance>();

            _cfApiClient = services.GetRequiredService<ICfApiClient>();
            cfCliService = services.GetRequiredService<ICfCliService>();
            _fileLocatorService = services.GetRequiredService<IFileLocatorService>();
        }

        public void AddCloudFoundryInstance(string name, string apiAddress, string accessToken)
        {
            if (CloudFoundryInstances.ContainsKey(name)) throw new Exception($"The name {name} already exists.");
            CloudFoundryInstances.Add(name, new CloudFoundryInstance(name, apiAddress, accessToken));
        }

        public async Task<ConnectResult> ConnectToCFAsync(string targetApiAddress, string username, SecureString password, string httpProxy, bool skipSsl)
        {
            if (string.IsNullOrEmpty(targetApiAddress)) throw new ArgumentException(nameof(targetApiAddress));

            if (string.IsNullOrEmpty(username)) throw new ArgumentException(nameof(username));

            if (password == null) throw new ArgumentNullException(nameof(password));

            try
            {
                DetailedResult targetResult = cfCliService.TargetApi(targetApiAddress, skipSsl);

                if (targetResult.CmdDetails == null)
                {
                    throw new Exception(
                        message: LoginFailureMessage,
                        innerException: new Exception(
                            message: $"Unable to connect to CF CLI"));
                }
                else if (targetResult.CmdDetails.ExitCode != 0)
                {
                    throw new Exception(
                        message: LoginFailureMessage,
                        innerException: new Exception(
                            message: $"Unable to target api at this address: {targetApiAddress}",
                            innerException: new Exception(targetResult.CmdDetails.StdErr)));
                }

                DetailedResult authResult = await cfCliService.AuthenticateAsync(username, password);

                if (authResult.CmdDetails == null)
                {
                    throw new Exception(
                       message: LoginFailureMessage,
                       innerException: new Exception(
                           message: $"Unable to connect to Cf CLI"));
                }
                else if (authResult.CmdDetails.ExitCode != 0)
                {
                    throw new Exception(
                       message: LoginFailureMessage,
                       innerException: new Exception(
                           message: $"Unable to authenticate user \"{username}\"",
                           innerException: new Exception(authResult.CmdDetails.StdErr)));
                }

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

        public async Task<List<CloudFoundryOrganization>> GetOrgsForCfInstanceAsync(CloudFoundryInstance cf)
        {

            List<CfCli.Models.Orgs.Org> orgsResults = await cfCliService.GetOrgsAsync();

            var orgs = new List<CloudFoundryOrganization>();
            if (orgsResults != null)
            {
                orgsResults.ForEach(delegate (CfCli.Models.Orgs.Org org)
                {
                    orgs.Add(new CloudFoundryOrganization(org.entity.name, org.metadata.guid, cf));
                });
            }

            return orgs;
        }

        public async Task<List<CloudFoundrySpace>> GetSpacesForOrgAsync(CloudFoundryOrganization org)
        {
            var target = org.ParentCf.ApiAddress;
            var accessToken = org.ParentCf.AccessToken;

            List<CfCli.Models.Spaces.Space> spacesResults = await cfCliService.GetSpacesAsync();

            var spaces = new List<CloudFoundrySpace>();
            if (spacesResults != null)
            {
                spacesResults.ForEach(delegate (CfCli.Models.Spaces.Space space)
                {
                    spaces.Add(new CloudFoundrySpace(space.entity.name, space.metadata.guid, org));
                });
            }

            return spaces;
        }

        public async Task<List<CloudFoundryApp>> GetAppsForSpaceAsync(CloudFoundrySpace space)
        {
            var target = space.ParentOrg.ParentCf.ApiAddress;
            var accessToken = space.ParentOrg.ParentCf.AccessToken;

            List<App> appResults = await _cfApiClient.ListAppsForSpace(target, accessToken, space.SpaceId);

            var apps = new List<CloudFoundryApp>();
            if (appResults != null)
            {
                appResults.ForEach(delegate (App app)
                {
                    var appToAdd = new CloudFoundryApp(app.name, app.guid, space)
                    {
                        State = app.state
                    };
                    apps.Add(appToAdd);
                });
            }

            return apps;
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

        public async Task<bool> StopAppAsync(CloudFoundryApp app)
        {
            try
            {
                var target = app.ParentSpace.ParentOrg.ParentCf.ApiAddress;
                var token = app.ParentSpace.ParentOrg.ParentCf.AccessToken;

                bool appWasStopped = await _cfApiClient.StopAppWithGuid(target, token, app.AppId);

                if (appWasStopped) app.State = "STOPPED";
                return appWasStopped;
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine(e);
                return false;
            }
        }

        public async Task<bool> StartAppAsync(CloudFoundryApp app)
        {
            try
            {
                var target = app.ParentSpace.ParentOrg.ParentCf.ApiAddress;
                var token = app.ParentSpace.ParentOrg.ParentCf.AccessToken;

                bool appWasStarted = await _cfApiClient.StartAppWithGuid(target, token, app.AppId);

                if (appWasStarted) app.State = "STARTED";
                return appWasStarted;
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine(e);
                return false;
            }
        }

        public async Task<bool> DeleteAppAsync(CloudFoundryApp app)
        {
            try
            {
                var target = app.ParentSpace.ParentOrg.ParentCf.ApiAddress;
                var token = app.ParentSpace.ParentOrg.ParentCf.AccessToken;

                bool appWasDeleted = await _cfApiClient.DeleteAppWithGuid(target, token, app.AppId);

                if (appWasDeleted) app.State = "DELETED";
                return appWasDeleted;
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine(e);
                return false;
            }
        }

        public async Task<DetailedResult> DeployAppAsync(CloudFoundryInstance targetCf, CloudFoundryOrganization targetOrg, CloudFoundrySpace targetSpace, string appName, string appProjPath, StdOutDelegate stdOutCallback, StdErrDelegate stdErrCallback)
        {
            if (!_fileLocatorService.DirContainsFiles(appProjPath)) return new DetailedResult(false, emptyOutputDirMessage);

            DetailedResult cfTargetResult = await cfCliService.InvokeCfCliAsync(arguments: $"target -o {targetOrg.OrgName} -s {targetSpace.SpaceName}", stdOutCallback, stdErrCallback);
            if (!cfTargetResult.Succeeded) return new DetailedResult(false, $"Unable to target org '{targetOrg.OrgName}' or space '{targetSpace.SpaceName}'.\n{cfTargetResult.Explanation}");

            DetailedResult cfPushResult = await cfCliService.InvokeCfCliAsync(arguments: "push " + appName, stdOutCallback, stdErrCallback, workingDir: appProjPath);
            if (!cfPushResult.Succeeded) return new DetailedResult(false, $"Successfully targeted org '{targetOrg.OrgName}' and space '{targetSpace.SpaceName}' but app deployment failed at the `cf push` stage.\n{cfPushResult.Explanation}");

            return new DetailedResult(true, $"App successfully deploying to org '{targetOrg.OrgName}', space '{targetSpace.SpaceName}'...");
        }

    }
}