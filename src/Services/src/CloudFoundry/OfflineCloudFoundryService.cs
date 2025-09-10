using Microsoft.Extensions.DependencyInjection;
using Serilog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security;
using System.Threading.Tasks;
using Tanzu.Toolkit.Models;
using Tanzu.Toolkit.Services.File;
using Tanzu.Toolkit.Services.Logging;
using Tanzu.Toolkit.Services.Serialization;

namespace Tanzu.Toolkit.Services.CloudFoundry
{
    public class OfflineCloudFoundryService : ICloudFoundryService
    {
        private readonly IFileService _fileService;
        private readonly ILogger _logger;
        private readonly ISerializationService _serializationService;

        private static readonly CloudFoundryInstance _offlineCloudFoundryInstance = new CloudFoundryInstance("offline", "https://offline");
        private static readonly List<CloudFoundryOrganization> _cfOrgs = new List<CloudFoundryOrganization>();
        private static readonly CloudFoundryOrganization _offlineCloudFoundryOrg1 = new CloudFoundryOrganization("offline-org-1", Guid.NewGuid().ToString(), _offlineCloudFoundryInstance);
        private static readonly CloudFoundryOrganization _offlineCloudFoundryOrg2 = new CloudFoundryOrganization("offline-org-2", Guid.NewGuid().ToString(), _offlineCloudFoundryInstance);
        private static readonly List<CloudFoundrySpace> _cfSpaces = new List<CloudFoundrySpace>();
        private static readonly List<CloudFoundryApp> _cfApps = new List<CloudFoundryApp>();

        public OfflineCloudFoundryService(IServiceProvider services)
        {
            try
            {
                var logSvc = services.GetRequiredService<ILoggingService>();
                _logger = logSvc.Logger;

                _fileService = services.GetRequiredService<IFileService>();
                _serializationService = services.GetRequiredService<ISerializationService>();
            }
            catch (Exception ex)
            {
                _logger?.Error("Unable to construct {ClassName} due to an unattainable service: {ServiceException}", nameof(CloudFoundryService), ex);
            }

            _cfOrgs.Add(_offlineCloudFoundryOrg1);
            _cfOrgs.Add(_offlineCloudFoundryOrg2);
            foreach (var org in _cfOrgs)
            {
                for (var i = 1; i < 5; i++)
                {
                    _cfSpaces.Add(new CloudFoundrySpace($"offline-space-{i}", Guid.NewGuid().ToString(), org));
                }
            }

            foreach (var space in _cfSpaces)
            {
                for (var i = 1; i < 5; i++)
                {
                    _cfApps.Add(new CloudFoundryApp($"offline-app-{i}", Guid.NewGuid().ToString(), space, i % 2 == 0 ? "STOPPED" : "STARTED"));
                }
            }
        }

        public DetailedResult ConfigureForCf(CloudFoundryInstance cf)
        {
            return new DetailedResult(true, "This is a fake success message");
        }

        public DetailedResult TargetCfApi(string targetApiAddress, bool skipSsl)
        {
            return new DetailedResult(true, "This is a fake success message");
        }

        public async Task<DetailedResult> LoginWithCredentialsAsync(string username, SecureString password)
        {
            await Task.Delay(2000);
            return new DetailedResult(true, "This is a fake success message");
        }

        public async Task<DetailedResult<string>> GetSSOPromptAsync(string cfApiAddress, bool skipSsl = false)
        {
            await Task.Delay(1000);
            return new DetailedResult<string>("https://www.google.com/search?q=why+did+you+think+this+would+work", true, "This is a fake success message");

        }

        public async Task<DetailedResult> LoginWithSSOPasscodeAsync(string cfApiAddress, string passcode)
        {
            await Task.Delay(1000);
            return new DetailedResult(true, "This is a fake success message");
        }

        public async Task<DetailedResult<List<CloudFoundryOrganization>>> GetOrgsForCfInstanceAsync(CloudFoundryInstance cf, bool skipSsl = false, int retryAmount = 1)
        {
            await Task.Delay(500);
            return new DetailedResult<List<CloudFoundryOrganization>>(_cfOrgs, true, "This is a fake success message");
        }

        public async Task<DetailedResult<List<CloudFoundrySpace>>> GetSpacesForOrgAsync(CloudFoundryOrganization org, bool skipSsl = false, int retryAmount = 1)
        {
            await Task.Delay(500);
            return new DetailedResult<List<CloudFoundrySpace>>(_cfSpaces.Where(space => space.ParentOrg == org).ToList(), true, "This is a fake success message");
        }

        public async Task<DetailedResult<List<CloudFoundryApp>>> GetAppsForSpaceAsync(CloudFoundrySpace space, bool skipSsl = false, int retryAmount = 1)
        {
            await Task.Delay(500);
            return new DetailedResult<List<CloudFoundryApp>>(_cfApps.Where(app => app.ParentSpace == space).ToList(), true, "This is a fake success message");
        }

        private readonly List<CloudFoundryApp> _allApps = new List<CloudFoundryApp>();

        public async Task<DetailedResult<List<CloudFoundryApp>>> ListAllAppsAsync(int retryAmount = 1)
        {
            await Task.Delay(1000);
            return new DetailedResult<List<CloudFoundryApp>>(_allApps, true, "This is a fake success message");
        }

        public async Task<DetailedResult<List<CfBuildpack>>> GetBuildpacksAsync(string apiAddress, int retryAmount = 1)
        {
            await Task.Delay(1000);
            return new DetailedResult<List<CfBuildpack>>(new List<CfBuildpack>
            {
                new CfBuildpack{Name = "offline", Stack = "cflinuxfs50"}
            }, true, "This is a fake success message");
        }

        public async Task<DetailedResult<List<CfService>>> GetServicesAsync(string apiAddress, int retryAmount = 1)
        {
            await Task.Delay(2000);
            return new DetailedResult<List<CfService>>(new List<CfService>(), true, "This is a fake success message");
        }

        public async Task<DetailedResult> StopAppAsync(CloudFoundryApp app, bool skipSsl = false, int retryAmount = 1)
        {

            app.State = "STOPPED";
            await Task.Delay(1000);
            return new DetailedResult(true, "This is a fake success message");
        }

        public async Task<DetailedResult> StartAppAsync(CloudFoundryApp app, bool skipSsl = false, int retryAmount = 1)
        {
            app.State = "STARTED";
            await Task.Delay(1000);
            return new DetailedResult(true, "This is a fake success message");
        }

        public async Task<DetailedResult> DeleteAppAsync(CloudFoundryApp app, bool skipSsl = false, bool removeRoutes = false, int retryAmount = 1)
        {
            app.State = "DELETED";
            await Task.Delay(1000);
            return new DetailedResult { Succeeded = true };
        }

        public async Task<DetailedResult<List<CloudFoundryRoute>>> GetRoutesForAppAsync(CloudFoundryApp app, int retryAmount = 1)
        {
            await Task.Delay(2000);
            return new DetailedResult<List<CloudFoundryRoute>> { Succeeded = true, Content = new List<CloudFoundryRoute>() };
        }

        public async Task<DetailedResult> DeleteAllRoutesForAppAsync(CloudFoundryApp app)
        {
            await Task.Delay(2000);
            return new DetailedResult { Succeeded = true };
        }

        public async Task<DetailedResult> DeployAppAsync(AppManifest appManifest, string defaultAppPath, CloudFoundryInstance targetCf, CloudFoundryOrganization targetOrg,
            CloudFoundrySpace targetSpace, Action<string> stdOutCallback, Action<string> stdErrCallback)
        {
            await Task.Delay(2000);
            return new DetailedResult(true, $"App successfully deploying to org '{targetOrg.OrgName}', space '{targetSpace.SpaceName}'...");
        }

        public async Task<DetailedResult<string>> GetRecentLogsAsync(CloudFoundryApp app)
        {
            await Task.Delay(2000);
            return new DetailedResult<string>("Fake app logs", true, "This is a fake success message");
        }

        public DetailedResult<Process> StreamAppLogs(CloudFoundryApp app, Action<string> stdOutCallback, Action<string> stdErrCallback)
        {
            throw new NotImplementedException();
        }

        public DetailedResult CreateManifestFile(string location, AppManifest manifest)
        {
            try
            {
                var ymlContents = _serializationService.SerializeCfAppManifest(manifest);

                _fileService.WriteTextToFile(location, "---\n" + ymlContents);

                return new DetailedResult { Succeeded = true };
            }
            catch (Exception ex)
            {
                return new DetailedResult { Succeeded = false, Explanation = ex.Message };
            }
        }

        public async Task<DetailedResult<List<string>>> GetStackNamesAsync(CloudFoundryInstance cf, int retryAmount = 1)
        {
            await Task.Delay(2000);
            return new DetailedResult<List<string>>(new List<string> { "cflinuxfs50", "magicalWindows" }, true, "This is a fake success message");
        }

        public void LogoutCfUser()
        {
        }
    }
}