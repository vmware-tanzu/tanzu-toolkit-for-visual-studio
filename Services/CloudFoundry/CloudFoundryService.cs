using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Security;
using System.Threading.Tasks;
using TanzuForVS.CloudFoundryApiClient;
using TanzuForVS.CloudFoundryApiClient.Models.AppsResponse;
using TanzuForVS.CloudFoundryApiClient.Models.OrgsResponse;
using TanzuForVS.CloudFoundryApiClient.Models.SpacesResponse;
using TanzuForVS.Models;
using TanzuForVS.Services.CfCli;
using TanzuForVS.Services.FileLocator;
using static TanzuForVS.Services.OutputHandler;

namespace TanzuForVS.Services.CloudFoundry
{
    public class CloudFoundryService : ICloudFoundryService
    {
        private static ICfApiClient _cfApiClient;
        private static ICfCliService _cfCliService;
        private static IFileLocatorService _fileLocatorService;
        internal const string emptyOutputDirMessage = "Unable to locate app files; project output directory is empty. (Has your project already been compiled?)";

        public string LoginFailureMessage { get; } = "Login failed.";
        public Dictionary<string, CloudFoundryInstance> CloudFoundryInstances { get; private set; }
        public CloudFoundryInstance ActiveCloud { get; set; }

        public CloudFoundryService(IServiceProvider services)
        {
            CloudFoundryInstances = new Dictionary<string, CloudFoundryInstance>();

            _cfApiClient = services.GetRequiredService<ICfApiClient>();
            _cfCliService = services.GetRequiredService<ICfCliService>();
            _fileLocatorService = services.GetRequiredService<IFileLocatorService>();
        }

        public void AddCloudFoundryInstance(string name, string apiAddress, string accessToken)
        {
            if (CloudFoundryInstances.ContainsKey(name)) throw new Exception($"The name {name} already exists.");
            CloudFoundryInstances.Add(name, new CloudFoundryInstance(name, apiAddress, accessToken));
        }

        public async Task<ConnectResult> ConnectToCFAsync(string target, string username, SecureString password, string httpProxy, bool skipSsl)
        {
            if (string.IsNullOrEmpty(target)) throw new ArgumentException(nameof(target));

            if (string.IsNullOrEmpty(username)) throw new ArgumentException(nameof(username));

            if (password == null) throw new ArgumentNullException(nameof(password));

            try
            {
                string passwordStr = new System.Net.NetworkCredential(string.Empty, password).Password;
                string accessToken = await _cfApiClient.LoginAsync(target, username, passwordStr);

                //* Redundant call to `cf login` provisions the cli environment, enabling it to make 
                //* requests like `cf push`, `cf orgs` (which require user to be logged in).
                await LoginViaCfCli(target, username, skipSsl, passwordStr);

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
            var target = cf.ApiAddress;
            var accessToken = cf.AccessToken;

            List<Org> orgsResults = await _cfApiClient.ListOrgs(target, accessToken);

            var orgs = new List<CloudFoundryOrganization>();
            if (orgsResults != null)
            {
                orgsResults.ForEach(delegate (Org org)
                {
                    orgs.Add(new CloudFoundryOrganization(org.name, org.guid, cf));
                });
            }

            return orgs;
        }

        public async Task<List<CloudFoundrySpace>> GetSpacesForOrgAsync(CloudFoundryOrganization org)
        {
            var target = org.ParentCf.ApiAddress;
            var accessToken = org.ParentCf.AccessToken;

            List<Space> spacesResults = await _cfApiClient.ListSpacesForOrg(target, accessToken, org.OrgId);

            var spaces = new List<CloudFoundrySpace>();
            if (spacesResults != null)
            {
                spacesResults.ForEach(delegate (Space space)
                {
                    spaces.Add(new CloudFoundrySpace(space.name, space.guid, org));
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

        public async Task<DetailedResult> DeployAppAsync(CloudFoundryInstance targetCf, CloudFoundryOrganization targetOrg, CloudFoundrySpace targetSpace, string appName, string appProjPath, StdOutDelegate stdOutHandler)
        {
            if (!_fileLocatorService.DirContainsFiles(appProjPath)) return new DetailedResult(false, emptyOutputDirMessage);

            DetailedResult cfTargetResult = await _cfCliService.ExecuteCfCliCommandAsync(arguments: $"target -o {targetOrg.OrgName} -s {targetSpace.SpaceName}", stdOutHandler: stdOutHandler);
            if (!cfTargetResult.Succeeded) return new DetailedResult(false, $"Unable to target org '{targetOrg.OrgName}' or space '{targetSpace.SpaceName}'.\n{cfTargetResult.Explanation}");

            DetailedResult cfPushResult = await _cfCliService.ExecuteCfCliCommandAsync(arguments: "push " + appName, workingDir: appProjPath, stdOutHandler: stdOutHandler);
            if (!cfPushResult.Succeeded) return new DetailedResult(false, $"Successfully targeted org '{targetOrg.OrgName}' and space '{targetSpace.SpaceName}' but app deployment failed at the `cf push` stage.\n{cfPushResult.Explanation}");

            return new DetailedResult(true, $"App successfully deploying to org '{targetOrg.OrgName}', space '{targetSpace.SpaceName}'...");
        }

        /// <summary>
        /// Simulate a `cf login` command by combining `cf api` with `cf auth`.
        /// </summary>
        /// <param name="target"></param>
        /// <param name="username"></param>
        /// <param name="skipSsl"></param>
        /// <param name="passwordStr"></param>
        /// <returns></returns>
        private static async Task LoginViaCfCli(string target, string username, bool skipSsl, string passwordStr)
        {
            string cfApiCmdArgs = $"api {target}{(skipSsl ? " --skip-ssl-validation" : string.Empty)}";
            await _cfCliService.ExecuteCfCliCommandAsync(cfApiCmdArgs);

            await _cfCliService.ExecuteCfCliCommandAsync($"auth {username} {passwordStr}");
        }

    }
}