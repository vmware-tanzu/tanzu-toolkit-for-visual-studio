using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Security;
using System.Threading.Tasks;
using Tanzu.Toolkit.CloudFoundryApiClient;
using Tanzu.Toolkit.VisualStudio.Models;
using Tanzu.Toolkit.VisualStudio.Services.CfCli;
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

        public async Task<List<CloudFoundryOrganization>> GetOrgsForCfInstanceAsync(CloudFoundryInstance cf, bool skipSsl = true)
        {
            var targetApiResult = cfCliService.TargetApi(cf.ApiAddress, skipSsl);
            if (!targetApiResult.Succeeded || targetApiResult.CmdDetails.ExitCode != 0) return new List<CloudFoundryOrganization>();

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

        public async Task<List<CloudFoundrySpace>> GetSpacesForOrgAsync(CloudFoundryOrganization org, bool skipSsl = true)
        {
            var targetApiResult = cfCliService.TargetApi(org.ParentCf.ApiAddress, skipSsl);
            if (!targetApiResult.Succeeded || targetApiResult.CmdDetails.ExitCode != 0) return new List<CloudFoundrySpace>();

            var targetOrgResult = cfCliService.TargetOrg(org.OrgName);
            if (!targetOrgResult.Succeeded || targetOrgResult.CmdDetails.ExitCode != 0) return new List<CloudFoundrySpace>();

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

        public async Task<List<CloudFoundryApp>> GetAppsForSpaceAsync(CloudFoundrySpace space, bool skipSsl = true)
        {
            var targetApiResult = cfCliService.TargetApi(space.ParentOrg.ParentCf.ApiAddress, skipSsl);
            if (!targetApiResult.Succeeded || targetApiResult.CmdDetails.ExitCode != 0) return new List<CloudFoundryApp>();

            var targetOrgResult = cfCliService.TargetOrg(space.ParentOrg.OrgName);
            if (!targetOrgResult.Succeeded || targetOrgResult.CmdDetails.ExitCode != 0) return new List<CloudFoundryApp>();

            var targetSpaceResult = cfCliService.TargetSpace(space.SpaceName);
            if (!targetSpaceResult.Succeeded || targetSpaceResult.CmdDetails.ExitCode != 0) return new List<CloudFoundryApp>();


            List<CfCli.Models.Apps.App> appResults = await cfCliService.GetAppsAsync();

            var apps = new List<CloudFoundryApp>();
            if (appResults != null)
            {
                appResults.ForEach(delegate (CfCli.Models.Apps.App app)
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

        public async Task<bool> StopAppAsync(CloudFoundryApp app, bool skipSsl = true)
        {
            var targetApiResult = cfCliService.TargetApi(app.ParentSpace.ParentOrg.ParentCf.ApiAddress, skipSsl);
            if (!targetApiResult.Succeeded || targetApiResult.CmdDetails.ExitCode != 0) return false;

            var targetOrgResult = cfCliService.TargetOrg(app.ParentSpace.ParentOrg.OrgName);
            if (!targetOrgResult.Succeeded || targetOrgResult.CmdDetails.ExitCode != 0) return false;

            var targetSpaceResult = cfCliService.TargetSpace(app.ParentSpace.SpaceName);
            if (!targetSpaceResult.Succeeded || targetSpaceResult.CmdDetails.ExitCode != 0) return false;

            DetailedResult stopResult = await cfCliService.StopAppByNameAsync(app.AppName);

            if (!stopResult.Succeeded || stopResult.CmdDetails.ExitCode != 0) return false;

            app.State = "STOPPED";
            return true;
        }
        
        public async Task<bool> StartAppAsync(CloudFoundryApp app, bool skipSsl = true)
        {
            var targetApiResult = cfCliService.TargetApi(app.ParentSpace.ParentOrg.ParentCf.ApiAddress, skipSsl);
            if (!targetApiResult.Succeeded || targetApiResult.CmdDetails.ExitCode != 0) return false;

            var targetOrgResult = cfCliService.TargetOrg(app.ParentSpace.ParentOrg.OrgName);
            if (!targetOrgResult.Succeeded || targetOrgResult.CmdDetails.ExitCode != 0) return false;

            var targetSpaceResult = cfCliService.TargetSpace(app.ParentSpace.SpaceName);
            if (!targetSpaceResult.Succeeded || targetSpaceResult.CmdDetails.ExitCode != 0) return false;

            DetailedResult startResult = await cfCliService.StartAppByNameAsync(app.AppName);

            if (!startResult.Succeeded || startResult.CmdDetails.ExitCode != 0) return false;

            app.State = "STARTED";
            return true;
        }

        public async Task<bool> DeleteAppAsync(CloudFoundryApp app, bool skipSsl = true, bool removeRoutes = true)
        {
            var targetApiResult = cfCliService.TargetApi(app.ParentSpace.ParentOrg.ParentCf.ApiAddress, skipSsl);
            if (!targetApiResult.Succeeded || targetApiResult.CmdDetails.ExitCode != 0) return false;

            var targetOrgResult = cfCliService.TargetOrg(app.ParentSpace.ParentOrg.OrgName);
            if (!targetOrgResult.Succeeded || targetOrgResult.CmdDetails.ExitCode != 0) return false;

            var targetSpaceResult = cfCliService.TargetSpace(app.ParentSpace.SpaceName);
            if (!targetSpaceResult.Succeeded || targetSpaceResult.CmdDetails.ExitCode != 0) return false;

            DetailedResult deleteResult = await cfCliService.DeleteAppByNameAsync(app.AppName, removeRoutes);

            if (!deleteResult.Succeeded || deleteResult.CmdDetails.ExitCode != 0) return false;

            app.State = "DELETED";
            return true;
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