using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Security;
using System.Threading.Tasks;
using Tanzu.Toolkit.VisualStudio.Models;
using Tanzu.Toolkit.VisualStudio.Services.CfCli;
using Tanzu.Toolkit.VisualStudio.Services.FileLocator;
using static Tanzu.Toolkit.VisualStudio.Services.OutputHandler.OutputHandler;

namespace Tanzu.Toolkit.VisualStudio.Services.CloudFoundry
{
    public class CloudFoundryService : ICloudFoundryService
    {
        private static ICfCliService cfCliService;
        private static IFileLocatorService _fileLocatorService;
        internal const string emptyOutputDirMessage = "Unable to locate app files; project output directory is empty. (Has your project already been compiled?)";

        public string LoginFailureMessage { get; } = "Login failed.";
        public Dictionary<string, CloudFoundryInstance> CloudFoundryInstances { get; internal set; }
        public CloudFoundryInstance ActiveCloud { get; set; }

        public CloudFoundryService(IServiceProvider services)
        {
            CloudFoundryInstances = new Dictionary<string, CloudFoundryInstance>();

            cfCliService = services.GetRequiredService<ICfCliService>();
            _fileLocatorService = services.GetRequiredService<IFileLocatorService>();
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

        public async Task<DetailedResult<List<CloudFoundryOrganization>>> GetOrgsForCfInstanceAsync(CloudFoundryInstance cf, bool skipSsl = true)
        {
            var targetApiResult = cfCliService.TargetApi(cf.ApiAddress, skipSsl);
            if (!targetApiResult.Succeeded)
            {
                return new DetailedResult<List<CloudFoundryOrganization>>(
                        content: null,
                        succeeded: false,
                        explanation: targetApiResult.Explanation,
                        cmdDetails: targetApiResult.CmdDetails);
            }

            DetailedResult<List<CfCli.Models.Orgs.Org>> orgsDetailedResult = await cfCliService.GetOrgsAsync();

            if (!orgsDetailedResult.Succeeded || orgsDetailedResult.Content == null)
            {
                return new DetailedResult<List<CloudFoundryOrganization>>(
                        content: null,
                        succeeded: false,
                        explanation: orgsDetailedResult.Explanation,
                        cmdDetails: orgsDetailedResult.CmdDetails);
            }

            var orgs = new List<CloudFoundryOrganization>();
            {
                orgsDetailedResult.Content.ForEach(delegate (CfCli.Models.Orgs.Org org)
                {
                    orgs.Add(new CloudFoundryOrganization(
                        org.entity.name, 
                        org.metadata.guid, 
                        cf,
                        org.entity.spaces_url
                    ));
                });
            }

            return new DetailedResult<List<CloudFoundryOrganization>>(
                succeeded: true,
                content: orgs,
                explanation: null,
                cmdDetails: orgsDetailedResult.CmdDetails);
        }

        public async Task<DetailedResult<List<CloudFoundrySpace>>> GetSpacesForOrgAsync(CloudFoundryOrganization org, bool skipSsl = true)
        {
            var targetApiResult = cfCliService.TargetApi(org.ParentCf.ApiAddress, skipSsl);
            if (!targetApiResult.Succeeded)
            {
                return new DetailedResult<List<CloudFoundrySpace>>(
                        content: null,
                        succeeded: false,
                        explanation: targetApiResult.Explanation,
                        cmdDetails: targetApiResult.CmdDetails);
            }

            var targetOrgResult = cfCliService.TargetOrg(org.OrgName);
            if (!targetOrgResult.Succeeded)
            {
                return new DetailedResult<List<CloudFoundrySpace>>(
                        content: null,
                        succeeded: false,
                        explanation: targetOrgResult.Explanation,
                        cmdDetails: targetOrgResult.CmdDetails);
            }

            DetailedResult<List<CfCli.Models.Spaces.Space>> spacesDetailedResult = await cfCliService.GetSpacesAsync();

            if (!spacesDetailedResult.Succeeded || spacesDetailedResult.Content == null)
            {
                return new DetailedResult<List<CloudFoundrySpace>>(
                        content: null,
                        succeeded: false,
                        explanation: spacesDetailedResult.Explanation,
                        cmdDetails: spacesDetailedResult.CmdDetails);
            }

            var spaces = new List<CloudFoundrySpace>();
            spacesDetailedResult.Content.ForEach(delegate (CfCli.Models.Spaces.Space space)
            {
                spaces.Add(new CloudFoundrySpace(space.entity.name, space.metadata.guid, org, space.entity.apps_url));
            });

            return new DetailedResult<List<CloudFoundrySpace>>(
                    succeeded: true,
                    content: spaces,
                    explanation: null,
                    cmdDetails: spacesDetailedResult.CmdDetails);
        }

        public async Task<DetailedResult<List<CloudFoundryApp>>> GetAppsForSpaceAsync(CloudFoundrySpace space, bool skipSsl = true)
        {
            var targetApiResult = cfCliService.TargetApi(space.ParentOrg.ParentCf.ApiAddress, skipSsl);
            if (!targetApiResult.Succeeded)
            {
                return new DetailedResult<List<CloudFoundryApp>>(
                    succeeded: false,
                    content: null,
                    explanation: targetApiResult.Explanation,
                    cmdDetails: targetApiResult.CmdDetails);
            }

            DetailedResult<List<CfCli.Models.Apps.App>> appsDetailedResult = await cfCliService.GetAppsAsync(space.AppsUrl);
            
            if (!appsDetailedResult.Succeeded || appsDetailedResult.Content == null)
            {
                return new DetailedResult<List<CloudFoundryApp>>(
                        content: null,
                        succeeded: false,
                        explanation: appsDetailedResult.Explanation,
                        cmdDetails: appsDetailedResult.CmdDetails);
            }

            var apps = new List<CloudFoundryApp>();
            appsDetailedResult.Content.ForEach(delegate (CfCli.Models.Apps.App app)
            {
                var appToAdd = new CloudFoundryApp(app.entity.name, app.metadata.guid, space)
                {
                    State = app.entity.state
                };
                apps.Add(appToAdd);
            });

            return new DetailedResult<List<CloudFoundryApp>>(
                succeeded: true,
                content: apps,
                explanation: null,
                cmdDetails: appsDetailedResult.CmdDetails);
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

        public async Task<DetailedResult> StopAppAsync(CloudFoundryApp app, bool skipSsl = true)
        {
            var targetApiResult = cfCliService.TargetApi(app.ParentSpace.ParentOrg.ParentCf.ApiAddress, skipSsl);
            if (!targetApiResult.Succeeded) return new DetailedResult(false, targetApiResult.Explanation, targetApiResult.CmdDetails);

            var targetOrgResult = cfCliService.TargetOrg(app.ParentSpace.ParentOrg.OrgName);
            if (!targetOrgResult.Succeeded) return new DetailedResult(false, targetOrgResult.Explanation, targetOrgResult.CmdDetails);

            var targetSpaceResult = cfCliService.TargetSpace(app.ParentSpace.SpaceName);
            if (!targetSpaceResult.Succeeded) return new DetailedResult(false, targetSpaceResult.Explanation, targetSpaceResult.CmdDetails);

            DetailedResult stopResult = await cfCliService.StopAppByNameAsync(app.AppName);

            if (!stopResult.Succeeded) return new DetailedResult(false, stopResult.Explanation, stopResult.CmdDetails);

            app.State = "STOPPED";
            return stopResult;
        }

        public async Task<DetailedResult> StartAppAsync(CloudFoundryApp app, bool skipSsl = true)
        {
            var targetApiResult = cfCliService.TargetApi(app.ParentSpace.ParentOrg.ParentCf.ApiAddress, skipSsl);
            if (!targetApiResult.Succeeded) return new DetailedResult(false, targetApiResult.Explanation, targetApiResult.CmdDetails);

            var targetOrgResult = cfCliService.TargetOrg(app.ParentSpace.ParentOrg.OrgName);
            if (!targetOrgResult.Succeeded) return new DetailedResult(false, targetOrgResult.Explanation, targetOrgResult.CmdDetails);

            var targetSpaceResult = cfCliService.TargetSpace(app.ParentSpace.SpaceName);
            if (!targetSpaceResult.Succeeded) return new DetailedResult(false, targetSpaceResult.Explanation, targetSpaceResult.CmdDetails);

            DetailedResult startResult = await cfCliService.StartAppByNameAsync(app.AppName);

            if (!startResult.Succeeded) return new DetailedResult(false, startResult.Explanation, startResult.CmdDetails);

            app.State = "STARTED";
            return startResult;
        }

        public async Task<DetailedResult> DeleteAppAsync(CloudFoundryApp app, bool skipSsl = true, bool removeRoutes = true)
        {
            var targetApiResult = cfCliService.TargetApi(app.ParentSpace.ParentOrg.ParentCf.ApiAddress, skipSsl);
            if (!targetApiResult.Succeeded) return new DetailedResult(false, targetApiResult.Explanation, targetApiResult.CmdDetails);

            var targetOrgResult = cfCliService.TargetOrg(app.ParentSpace.ParentOrg.OrgName);
            if (!targetOrgResult.Succeeded) return new DetailedResult(false, targetOrgResult.Explanation, targetOrgResult.CmdDetails);

            var targetSpaceResult = cfCliService.TargetSpace(app.ParentSpace.SpaceName);
            if (!targetSpaceResult.Succeeded) return new DetailedResult(false, targetSpaceResult.Explanation, targetSpaceResult.CmdDetails);

            DetailedResult deleteResult = await cfCliService.DeleteAppByNameAsync(app.AppName, removeRoutes);

            if (!deleteResult.Succeeded) return new DetailedResult(false, deleteResult.Explanation, deleteResult.CmdDetails);

            app.State = "DELETED";
            return deleteResult;
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

    }
}