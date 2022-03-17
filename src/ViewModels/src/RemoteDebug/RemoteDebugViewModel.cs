using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Tanzu.Toolkit.Models;
using Tanzu.Toolkit.Services.CfCli;
using Tanzu.Toolkit.Services.CloudFoundry;
using Tanzu.Toolkit.Services.DotnetCli;

namespace Tanzu.Toolkit.ViewModels.RemoteDebug
{
    public class RemoteDebugViewModel : AbstractViewModel, IRemoteDebugViewModel
    {
        private readonly ITasExplorerViewModel _tasExplorer;
        private ICloudFoundryService _cfClient;
        private readonly ICfCliService _cfCliService;
        private readonly IDotnetCliService _dotnetCliService;
        private readonly string _expectedAppName;
        private readonly string _pathToProjectRootDir;
        private readonly string _targetFrameworkMoniker;
        private List<CloudFoundryApp> _accessibleApps;
        private string _dialogMessage;
        private string _loadingMessage;
        private CloudFoundryApp _appToDebug;
        private bool _debugExistingApp;
        private bool _pushNewAppToDebug;
        private string _option1Text;
        private string _option2Text;
        private List<CloudFoundryOrganization> _orgOptions;
        private List<CloudFoundrySpace> _spaceOptions;
        private CloudFoundryOrganization _selectedOrg;
        private CloudFoundrySpace _selectedSpace;
        private CloudFoundryApp _selectedApp;

        public RemoteDebugViewModel(string expectedAppName, string pathToProjectRootDir, string targetFrameworkMoniker, IServiceProvider services) : base(services)
        {
            _expectedAppName = expectedAppName;
            _pathToProjectRootDir = pathToProjectRootDir;
            _targetFrameworkMoniker = targetFrameworkMoniker;
            _tasExplorer = services.GetRequiredService<ITasExplorerViewModel>();
            _cfCliService = services.GetRequiredService<ICfCliService>();
            _dotnetCliService = services.GetRequiredService<IDotnetCliService>();

            if (_tasExplorer == null || _tasExplorer.TasConnection == null)
            {
                DialogService.ShowDialog(typeof(LoginViewModel).Name);
            }
            if (_tasExplorer == null || _tasExplorer.TasConnection == null)
            {
                Close();
                return;
            }

            _cfClient = _tasExplorer.TasConnection.CfClient;

            LoadingMessage = "Identifying remote app to debug...";
            Option1Text = $"Push new version of \"{_expectedAppName}\" to debug";
            Option2Text = $"Select existing app to debug";
            AppToDebug = null;

            var _ = InitiateRemoteDebuggingAsync();
        }

        // Properties //

        public List<CloudFoundryApp> AccessibleApps
        {
            get => _accessibleApps;

            set
            {
                _accessibleApps = value;
                RaisePropertyChangedEvent("AccessibleApps");
            }
        }

        public CloudFoundryApp AppToDebug
        {
            get => _appToDebug;
            set
            {
                _appToDebug = value;
                RaisePropertyChangedEvent("AppToDebug");
            }
        }

        public CloudFoundryApp SelectedApp
        {
            get => _selectedApp;
            set
            {
                _selectedApp = value;
                RaisePropertyChangedEvent("SelectedApp");
            }
        }

        public bool DebugExistingApp
        {
            get => _debugExistingApp;
            set
            {
                if (value)
                {
                    _debugExistingApp = true;
                    PushNewAppToDebug = false;
                }
                else if (PushNewAppToDebug)
                {
                    _debugExistingApp = false;
                }
                RaisePropertyChangedEvent("DebugExistingApp");
            }
        }

        public bool PushNewAppToDebug
        {
            get => _pushNewAppToDebug;
            set
            {
                if (value)
                {
                    _pushNewAppToDebug = true;
                    DebugExistingApp = false;
                }
                else if (DebugExistingApp)
                {
                    _pushNewAppToDebug = false;
                }
                RaisePropertyChangedEvent("PushNewAppToDebug");
            }
        }

        public string DialogMessage
        {
            get => _dialogMessage;

            set
            {
                _dialogMessage = value;
                RaisePropertyChangedEvent("DialogMessage");
            }
        }

        public string Option1Text
        {
            get => _option1Text;
            set
            {
                _option1Text = value;
                RaisePropertyChangedEvent("Option1Text");
            }
        }

        public string Option2Text
        {
            get => _option2Text;
            set
            {
                _option2Text = value;
                RaisePropertyChangedEvent("Option2Text");
            }
        }

        public List<CloudFoundryOrganization> OrgOptions
        {
            get => _orgOptions;

            set
            {
                _orgOptions = value;
                RaisePropertyChangedEvent("OrgOptions");
            }
        }

        public List<CloudFoundrySpace> SpaceOptions
        {
            get => _spaceOptions;

            set
            {
                _spaceOptions = value;
                RaisePropertyChangedEvent("SpaceOptions");
            }
        }

        public CloudFoundryOrganization SelectedOrg
        {
            get => _selectedOrg;
            set
            {
                _selectedOrg = value;
                var _ = UpdateCfSpaceOptions();
                RaisePropertyChangedEvent("SelectedOrg");
            }
        }

        public CloudFoundrySpace SelectedSpace
        {
            get => _selectedSpace;
            set
            {
                _selectedSpace = value;
                RaisePropertyChangedEvent("SelectedSpace");
            }
        }

        public string LoadingMessage
        {
            get => _loadingMessage;

            set
            {
                _loadingMessage = value;
                RaisePropertyChangedEvent("LoadingMessage");
            }
        }

        // Methods //

        public async Task InitiateRemoteDebuggingAsync()
        {
            if (_tasExplorer != null && _tasExplorer.TasConnection != null)
            {
                await PopulateAccessibleAppsAsync();

                AppToDebug = AccessibleApps.FirstOrDefault(app => app.AppName == _expectedAppName);
                if (AppToDebug == null)
                {
                    LoadingMessage = null;
                    await UpdateCfOrgOptions();
                    DialogMessage = $"No app found with a name matching \"{_expectedAppName}\"";
                }
            }
        }

        public async Task ProceedToDebug(object arg = null)
        {
            if (PushNewAppToDebug)
            {
                await PushNewAppWithDebugConfiguration();
            }
            else if (DebugExistingApp)
            {
                AppToDebug = SelectedApp;
            }
            else
            {
                var msg = "Encountered unexpected debug strategy";
                Logger.Error(msg);
                ErrorService.DisplayErrorDialog(string.Empty, msg);
                Close();
            }
        }

        public bool CheckForRemoteDebugAgent()
        {
            throw new NotImplementedException();
        }

        public void InstallRemoteDebugAgent()
        {
            throw new NotImplementedException();
        }

        public bool CheckForLaunchFile()
        {
            throw new NotImplementedException();
        }

        public void CreateLaunchFile()
        {
            throw new NotImplementedException();
        }

        public async Task UpdateCfOrgOptions()
        {
            if (_tasExplorer.TasConnection == null)
            {
                OrgOptions = new List<CloudFoundryOrganization>();
            }
            else
            {
                var orgsResponse = await _tasExplorer.TasConnection.CfClient.GetOrgsForCfInstanceAsync(_tasExplorer.TasConnection.CloudFoundryInstance);
                if (orgsResponse.Succeeded)
                {
                    OrgOptions = orgsResponse.Content;
                }
                else
                {
                    Logger.Error("RemoteDebugViewModel failed to get orgs. {OrgsResponse}", orgsResponse);
                    ErrorService.DisplayErrorDialog("Unable to retrieve orgs", orgsResponse.Explanation);
                }
            }
        }

        public async Task UpdateCfSpaceOptions()
        {
            if (SelectedOrg == null || _tasExplorer.TasConnection == null)
            {
                SpaceOptions = new List<CloudFoundrySpace>();
            }
            else
            {
                var spacesResponse = await _tasExplorer.TasConnection.CfClient.GetSpacesForOrgAsync(SelectedOrg);

                if (spacesResponse.Succeeded)
                {
                    SpaceOptions = spacesResponse.Content;
                }
                else
                {
                    Logger.Error("RemoteDebugViewModel failed to get spaces. {SpacesResponse}", spacesResponse);
                    ErrorService.DisplayErrorDialog("Unable to retrieve spaces", spacesResponse.Explanation);
                }
            }
        }

        public void Close()
        {
            DialogService.CloseDialogByName(nameof(RemoteDebugViewModel));
        }

        private async Task PushNewAppWithDebugConfiguration()
        {
            var runtimeIdentifier = "linux-x64";
            var publishConfiguration = "Debug";
            var publishDirName = "publish";

            var errorString = string.Empty;
            var outputString = string.Empty;
            void AccumulateStdOut(string s)
            {
                outputString += s;
            };
            void AccumulateStdErr(string s)
            {
                errorString += s;
            };

            var publishSucceeded = await _dotnetCliService.PublishProjectForRemoteDebuggingAsync(_pathToProjectRootDir, _targetFrameworkMoniker, runtimeIdentifier, publishConfiguration, publishDirName, StdOutCallback: AccumulateStdOut, StdErrCallback: AccumulateStdErr);
            if (!string.IsNullOrEmpty(errorString))
            {
                var msg = $"Project failed to publish with error: \"{errorString}\"";
                Logger.Error("Unable to intitate remote debugging; project failed to publish, {PublishError}", errorString);
                ErrorService.DisplayErrorDialog("Unable to intitate remote debugging", msg);
            }
            else if (!publishSucceeded)
            {
                Logger.Error("Unable to intitate remote debugging; project failed to publish. Publish command output: {PublishOutput}", outputString);
                ErrorService.DisplayErrorDialog("Unable to intitate remote debugging", "Project failed to publish");
                return;
            }

            var pathToPublishDir = Path.Combine(_pathToProjectRootDir, publishDirName);
            var appConfig = new AppManifest
            {
                Applications = new List<AppConfig>
                    {
                        new AppConfig
                        {
                            Name = _expectedAppName,
                            Path = pathToPublishDir,
                        }
                    }
            };

            var pushResult = await _cfClient.DeployAppAsync(appConfig, pathToPublishDir, _tasExplorer.TasConnection.CloudFoundryInstance, SelectedOrg, SelectedSpace, null, null);
            if (!pushResult.Succeeded)
            {
                var msg = $"Failed to push app '{_expectedAppName}'; {pushResult.Explanation}";
                Logger.Error(msg);
                ErrorService.DisplayErrorDialog("Unable to initiate remote debugging", msg);
            }
        }

        private async Task PopulateAccessibleAppsAsync()
        {
            var appsResult = await _cfClient.ListAllAppsAsync();
            if (appsResult.Succeeded)
            {
                AccessibleApps = appsResult.Content;
            }
            else
            {
                var title = "Unable to initiate remote debugging";
                var msg = $"Something went wrong while querying apps on {_tasExplorer.TasConnection.CloudFoundryInstance.InstanceName}.\nIt may help to try disconnecting & signing into TAS again; if this issue persists, please contact tas-vs-extension@vmware.com";
                Logger.Error(title + "; " + msg);
                ErrorService.DisplayErrorDialog(title, msg);
                Close();
            }
        }

        // Predicates //

        public bool CanProceedToDebug(object arg = null)
        {
            return (PushNewAppToDebug && SelectedOrg != null && SelectedSpace != null)
                || (DebugExistingApp && AppToDebug != null);
        }
    }
}
