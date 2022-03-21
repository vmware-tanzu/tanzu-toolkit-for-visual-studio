using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Tanzu.Toolkit.Models;
using Tanzu.Toolkit.Services;
using Tanzu.Toolkit.Services.CfCli;
using Tanzu.Toolkit.Services.CloudFoundry;
using Tanzu.Toolkit.Services.DotnetCli;
using Tanzu.Toolkit.Services.File;

namespace Tanzu.Toolkit.ViewModels.RemoteDebug
{
    public class RemoteDebugViewModel : AbstractViewModel, IRemoteDebugViewModel
    {
        private readonly ITasExplorerViewModel _tasExplorer;
        private readonly ICloudFoundryService _cfClient;
        private readonly ICfCliService _cfCliService;
        private readonly IDotnetCliService _dotnetCliService;
        private readonly IFileService _fileService;
        private readonly IView _outputView;
        private IOutputViewModel _outputViewModel;
        private readonly string _expectedAppName;
        private readonly string _pathToProjectRootDir;
        private readonly string _targetFrameworkMoniker;
        private readonly string _expectedPathToLaunchFile;
        private readonly Action _initiateDebugCallback;
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
        private bool _waitingOnAppConfirmation = false;
        private bool _debugAgentInstalled;
        private bool _launchFileExists;
        private const string _vsdbgInstallationBaseDir = "/home/vcap/app";
        private const string _vsdbgDirName = "vsdbg";
        private const string _vsdbgExecutableName = "vsdbg";
        private readonly string _pathToVsdbgOnVM;
        public static readonly string _launchFileName = "launch.json";
        private readonly JsonSerializerOptions _serializationOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        public RemoteDebugViewModel(string expectedAppName, string pathToProjectRootDir, string targetFrameworkMoniker, string expectedPathToLaunchFile, Action initiateDebugCallback, IServiceProvider services) : base(services)
        {
            _expectedAppName = expectedAppName;
            _pathToProjectRootDir = pathToProjectRootDir;
            _targetFrameworkMoniker = targetFrameworkMoniker;
            _expectedPathToLaunchFile = expectedPathToLaunchFile;
            _initiateDebugCallback = initiateDebugCallback;
            _tasExplorer = services.GetRequiredService<ITasExplorerViewModel>();
            _cfCliService = services.GetRequiredService<ICfCliService>();
            _dotnetCliService = services.GetRequiredService<IDotnetCliService>();
            _fileService = services.GetRequiredService<IFileService>();

            _pathToVsdbgOnVM = _vsdbgInstallationBaseDir + "/" + _vsdbgDirName;

            _outputView = ViewLocatorService.GetViewByViewModelName(nameof(OutputViewModel)) as IView;
            _outputViewModel = _outputView?.ViewModel as IOutputViewModel;

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

            Option1Text = $"Push new version of \"{_expectedAppName}\" to debug";
            Option2Text = $"Select existing app to debug";
            AppToDebug = null;

            var _ = BeginRemoteDebuggingAsync();
        }

        // Properties //

        public Action ViewOpener { get; set; }

        public Action ViewCloser { get; set; }

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

        public async Task BeginRemoteDebuggingAsync()
        {
            await EstablishAppToDebugAsync();
            if (_waitingOnAppConfirmation)
            {
                // resolution delegated to UI;
                // once a decision is made, this method should be called
                // again after _waitingOnAppConfirmation is set to false
                return;
            }
            if (AppToDebug == null)
            {
                ErrorService.DisplayErrorDialog("Remote Debug Error", "Unable to identify app to debug.\n" +
                    "This is unexpected; it may help to sign out of TAS & try debugging again after logging back in.\n" +
                    "If this issue persists, please contact tas-vs-extension@vmware.com");
                Close();
                return;
            }

            await EnsureDebuggingAgentInstalledOnRemoteAsync();
            if (!_debugAgentInstalled)
            {
                ErrorService.DisplayErrorDialog("Remote Debug Error", "Failed to install remote debugging agent.\n" +
                    "It may help to sign out of TAS & try debugging again after logging back in.\n" +
                    "If this issue persists, please contact tas-vs-extension@vmware.com");
                Close();
                return;
            }

            CreateLaunchFileIfNonexistent();
            if (!_launchFileExists)
            {
                ErrorService.DisplayErrorDialog("Unable to bind to remote debugging agent.", $"Failed to specify launch configuration \"{_launchFileName}\".\n" +
                    $"It may help to try disconnecting & signing into TAS again; if this issue persists, please contact tas-vs-extension@vmware.com");
                Close();
                return;
            }

            _initiateDebugCallback?.Invoke();
            Close();
            FileService.DeleteFile(_expectedPathToLaunchFile);
        }

        public async Task EstablishAppToDebugAsync()
        {
            LoadingMessage = "Identifying remote app to debug...";
            await PopulateAccessibleAppsAsync();
            AppToDebug = AccessibleApps.FirstOrDefault(app => app.AppName == _expectedAppName);
            if (AppToDebug == null)
            {
                _waitingOnAppConfirmation = true;
                var _ = PromptAppResolutionAsync();
            }
        }

        public async Task ResolveMissingAppAsync(object arg = null)
        {
            _waitingOnAppConfirmation = false;

            if (PushNewAppToDebug)
            {
                Close();
                _outputView.Show();
                await PushNewAppWithDebugConfigurationAsync();
                var _ = BeginRemoteDebuggingAsync(); // start debug process over from beginning
                ViewOpener?.Invoke(); // reopen remote debug dialog
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

        public async Task EnsureDebuggingAgentInstalledOnRemoteAsync()
        {
            LoadingMessage = "Checking Debugging Agent...";
            DetailedResult sshResult;

            var sshCommand = $"ls {_pathToVsdbgOnVM}";
            try
            {
                sshResult = await _cfCliService.ExecuteSshCommand(AppToDebug.AppName, AppToDebug.ParentSpace.ParentOrg.OrgName, AppToDebug.ParentSpace.SpaceName, sshCommand);
            }
            catch (InvalidRefreshTokenException)
            {
                _tasExplorer.AuthenticationRequired = true;
                ErrorService.DisplayErrorDialog("Unable to initate remote debugging", $"Connection to {_tasExplorer.TasConnection.DisplayText} has expired; please log in again to re-authenticate.");
                Close();
                return;
            }
            catch (Exception ex)
            {
                Logger.Error("Something unexpected happened while initializing remote debugging agent: {CfSshException}", ex);
                ErrorService.DisplayErrorDialog("Unable to initate remote debugging", $"Something unexpected happened while initializing remote debugging agent. Please try again; if this issue persists, contact tas-vs-extension@vmware.com");
                Close();
                return;
            }

            var response = sshResult.CmdResult.StdOut;
            var sshQuerySucceeded = sshResult.Succeeded && response != null;
            if (!sshQuerySucceeded)
            {
                // TODO: clean up the error process here; should only see 1 error dialog in this error case (thrown from BeginRemoteDebuggingAsync)
                Logger.Error("Unable to verify remote debugging agent; couldn't connect to {AppName} via SSH.", AppToDebug.AppName);
                ErrorService.DisplayErrorDialog("Unable to verify remote debugging agent.", $"Couldn't connect to {AppToDebug.AppName} via SSH.");
                Close();
                return;
            }

            _debugAgentInstalled = response.Contains(_vsdbgExecutableName);
            if (!_debugAgentInstalled)
            {
                // TODO: find a way around lack of permissions for creating this directory 
                // e.g. create it during the publish step?
                // e.g. install the whole vsdbg tool during publish??

                var vsdbgVersion = "latest";
                DetailedResult vsdbgInstallationResult;
                try
                {
                    var installationSshCommand = $"curl -sSL https://aka.ms/getvsdbgsh | bash /dev/stdin -v {vsdbgVersion} -l {_pathToVsdbgOnVM}";

                    vsdbgInstallationResult = await _cfCliService.ExecuteSshCommand(AppToDebug.AppName, AppToDebug.ParentSpace.ParentOrg.OrgName, AppToDebug.ParentSpace.SpaceName, installationSshCommand);
                }
                catch (InvalidRefreshTokenException)
                {
                    // TODO: clean up the error process here; should only see 1 error dialog in this error case (thrown from BeginRemoteDebuggingAsync)
                    _tasExplorer.AuthenticationRequired = true;
                    ErrorService.DisplayErrorDialog("Unable to initate remote debugging", $"Connection to {_tasExplorer.TasConnection.DisplayText} has expired; please log in again to re-authenticate.");
                    Close();
                    return;
                }
                catch (Exception ex)
                {
                    // TODO: clean up the error process here; should only see 1 error dialog in this error case (thrown from BeginRemoteDebuggingAsync)
                    Logger.Error("Something unexpected happened while installing remote debugging agent: {VsdbgInstallationException}", ex);
                    ErrorService.DisplayErrorDialog("Unable to initate remote debugging", $"Something unexpected happened while installing remote debugging agent. Please try again; if this issue persists, contact tas-vs-extension@vmware.com");
                    Close();
                    return;
                }

                var installationSucceeded = vsdbgInstallationResult.Succeeded
                    && vsdbgInstallationResult.CmdResult != null
                    && vsdbgInstallationResult.CmdResult.StdOut != null
                    && vsdbgInstallationResult.CmdResult.StdOut.Contains($"Successfully installed vsdbg at '{_pathToVsdbgOnVM}'");

                if (installationSucceeded)
                {
                    _debugAgentInstalled = true;
                    Logger.Information($"Successfully installed remote debugging agent for app '{AppToDebug.AppName}' at {_pathToVsdbgOnVM}");
                }
                else
                {
                    // TODO: clean up the error process here; should only see 1 error dialog in this error case (thrown from BeginRemoteDebuggingAsync)
                    Logger.Error("Unable to install remote debugging agent: {VsdbgInstallationExplanation}", vsdbgInstallationResult.Explanation);
                    ErrorService.DisplayErrorDialog("Unable to initate remote debugging", $"Something unexpected happened while installing remote debugging agent. Please try again; if this issue persists, contact tas-vs-extension@vmware.com");
                    Close();
                    return;
                }
            }
        }

        public void CreateLaunchFileIfNonexistent()
        {
            _launchFileExists = false;
            try
            {
                if (!File.Exists(_expectedPathToLaunchFile))
                {
                    var launchFileConfig = new RemoteDebugLaunchConfig
                    {
                        version = "0.2.0",
                        adapter = "cf",
                        adapterArgs = $"ssh {AppToDebug.AppName} -c \"/tmp/lifecycle/shell {_vsdbgInstallationBaseDir} 'bash -c \\\"{_pathToVsdbgOnVM}/{_vsdbgExecutableName} --interpreter=vscode\\\"'\"",
                        languageMappings = new Languagemappings
                        {
                            CSharp = new CSharp
                            {
                                languageId = "3F5162F8-07C6-11D3-9053-00C04FA302A1",
                                extensions = new string[] { "*" },
                            },
                        },
                        exceptionCategoryMappings = new Exceptioncategorymappings
                        {
                            CLR = "449EC4CC-30D2-4032-9256-EE18EB41B62B",
                            MDA = "6ECE07A9-0EDE-45C4-8296-818D8FC401D4",
                        },
                        configurations = new Configuration[]
                        {
                            new Configuration
                            {
                                name = ".NET Core Launch",
                                type = "coreclr",
                                processName = AppToDebug.AppName,
                                request = "attach",
                                justMyCode = false,
                                cwd = _vsdbgInstallationBaseDir,
                                logging = new Logging
                                {
                                    engineLogging = true,
                                },
                            },
                        }
                    };
                    var newLaunchFileContents = JsonSerializer.Serialize(launchFileConfig, _serializationOptions);
                    _fileService.WriteTextToFile(_expectedPathToLaunchFile, newLaunchFileContents);
                }
                _launchFileExists = true;
            }
            catch (Exception ex)
            {
                Logger.Error("Failed to create launch file for remote debugging: {FileCreationException}", ex);
                _launchFileExists = false;
            }
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
            ViewCloser?.Invoke();
        }

        private async Task PromptAppResolutionAsync()
        {
            await UpdateCfOrgOptions();
            DialogMessage = $"No app found with a name matching \"{_expectedAppName}\"";
            LoadingMessage = null;
        }

        private async Task PushNewAppWithDebugConfigurationAsync()
        {
            var runtimeIdentifier = "linux-x64";
            var publishConfiguration = "Debug";
            var publishDirName = "publish";
            var publishTask = _dotnetCliService.PublishProjectForRemoteDebuggingAsync(
                _pathToProjectRootDir,
                _targetFrameworkMoniker,
                runtimeIdentifier,
                publishConfiguration,
                publishDirName,
                StdOutCallback: _outputViewModel.AppendLine,
                StdErrCallback: _outputViewModel.AppendLine);
            var publishSucceeded = await publishTask;
            if (!publishSucceeded)
            {
                Logger.Error("Unable to intitate remote debugging; project failed to publish.");
                _outputViewModel.AppendLine("Project failed to publish");
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
            var pushResult = await _cfClient.DeployAppAsync(
                appConfig,
                pathToPublishDir,
                _tasExplorer.TasConnection.CloudFoundryInstance,
                SelectedOrg,
                SelectedSpace,
                _outputViewModel.AppendLine,
                _outputViewModel.AppendLine);
            if (!pushResult.Succeeded)
            {
                var msg = $"Failed to push app '{_expectedAppName}'; {pushResult.Explanation}";
                Logger.Error(msg);
                _outputViewModel.AppendLine(msg);
                return;
            }
        }

        private async Task PopulateAccessibleAppsAsync()
        {
            try
            {
                var apps = new List<CloudFoundryApp>();
                var appListLock = new object();

                var orgsResult = await _cfClient.GetOrgsForCfInstanceAsync(_tasExplorer.TasConnection.CloudFoundryInstance);
                if (!orgsResult.Succeeded)
                {
                    var title = "Unable to initiate remote debugging";
                    var msg = $"Something went wrong while querying apps on {_tasExplorer.TasConnection.CloudFoundryInstance.InstanceName}.\nIt may help to try disconnecting & signing into TAS again; if this issue persists, please contact tas-vs-extension@vmware.com";
                    Logger.Error(title + "; " + "orgs request failed: {OrgsError}", orgsResult.Explanation);
                    ErrorService.DisplayErrorDialog(title, msg);
                    Close();
                    return;
                }

                var spaceTasks = new List<Task>();
                foreach (var org in orgsResult.Content)
                {
                    var getSpacesForOrgTask = Task.Run(async () =>
                    {
                        var spacesResult = await _cfClient.GetSpacesForOrgAsync(org);
                        if (spacesResult.Succeeded)
                        {
                            var appTasks = new List<Task>();
                            foreach (var space in spacesResult.Content)
                            {
                                var getAppsForSpaceTask = Task.Run(async () =>
                                {
                                    var appsResult = await _cfClient.GetAppsForSpaceAsync(space);
                                    if (appsResult.Succeeded)
                                    {
                                        foreach (var app in appsResult.Content)
                                        {
                                            lock (appListLock)
                                            {
                                                apps.Add(app);
                                            }
                                        }
                                    }
                                    else
                                    {
                                        Logger.Error("Apps request failed for space {SpaceName} while trying to query apps for remote debugging: {AppsError}", space.SpaceName, appsResult.Explanation);
                                    }
                                });
                                appTasks.Add(getAppsForSpaceTask);
                            }
                            await Task.WhenAll(appTasks);
                        }
                        else
                        {
                            Logger.Error("Spaces request failed for org {OrgName} while trying to query apps for remote debugging: {SpacesError}", org.OrgName, spacesResult.Explanation);
                        }
                    });
                    spaceTasks.Add(getSpacesForOrgTask);
                }

                await Task.WhenAll(spaceTasks);
                AccessibleApps = apps;
            }
            catch (Exception ex)
            {
                var title = "Unable to initiate remote debugging";
                var msg = $"Something unexpected happened while querying apps on {_tasExplorer.TasConnection.CloudFoundryInstance.InstanceName}.\nIt may help to try disconnecting & signing into TAS again; if this issue persists, please contact tas-vs-extension@vmware.com";
                Logger.Error(title + "; " + msg + "{RemoteDebugException}", ex);
                ErrorService.DisplayErrorDialog(title, msg);
                Close();
                return;
            }
        }

        // Predicates //

        public bool CanResolveMissingApp(object arg = null)
        {
            return (PushNewAppToDebug && SelectedOrg != null && SelectedSpace != null)
                || (DebugExistingApp && AppToDebug != null);
        }
    }

    public class RemoteDebugLaunchConfig
    {
        public string version { get; set; }
        public string adapter { get; set; }
        public string adapterArgs { get; set; }
        public Languagemappings languageMappings { get; set; }
        public Exceptioncategorymappings exceptionCategoryMappings { get; set; }
        public Configuration[] configurations { get; set; }
    }

    public class Languagemappings
    {
        [JsonPropertyName("C#")]
        public CSharp CSharp { get; set; }
    }

    public class CSharp
    {
        public string languageId { get; set; }
        public string[] extensions { get; set; }
    }

    public class Exceptioncategorymappings
    {
        public string CLR { get; set; }
        public string MDA { get; set; }
    }

    public class Configuration
    {
        public string name { get; set; }
        public string type { get; set; }
        public string processName { get; set; }
        public string request { get; set; }
        public bool justMyCode { get; set; }
        public string cwd { get; set; }
        public Logging logging { get; set; }
    }

    public class Logging
    {
        public bool engineLogging { get; set; }
    }
}
