using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
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
        private ICloudFoundryService _cfClient;
        private readonly ICfCliService _cfCliService;
        private readonly IDotnetCliService _dotnetCliService;
        private readonly IFileService _fileService;
        private readonly IView _outputView;
        private IOutputViewModel _outputViewModel;
        private readonly string _projectName;
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
        private bool _isLoggedIn;
        private string _option1Text;
        private string _option2Text;
        private List<CloudFoundryOrganization> _orgOptions;
        private List<CloudFoundrySpace> _spaceOptions;
        private List<string> _stackOptions;
        private CloudFoundryOrganization _selectedOrg;
        private CloudFoundrySpace _selectedSpace;
        private CloudFoundryApp _selectedApp;
        private string _selectedStack = "linux";
        private bool _waitingOnAppConfirmation = false;
        private bool _debugAgentInstalled;
        private bool _launchFileExists;
        private const string _appDirLinux = "/home/vcap/app";
        private const string _appDirWindows = "c:\\Users\\vcap\\app";
        private const string _vsdbgInstallationDirLinux = "/home/vcap/app/vsdbg";
        private const string _vsdbgInstallationDirWindows = "c:\\Users\\vcap\\app\\vsdbg";
        private const string _vsdbgExecutableNameLinux = "vsdbg";
        private const string _vsdbgExecutableNameWindows = "vsdbg.exe";
        private readonly string _vsdbgPathLinux;
        private readonly string _vsdbgPathWindows;
        public static readonly string _launchFileName = "launch.json";
        private readonly JsonSerializerOptions _serializationOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        public RemoteDebugViewModel(string expectedAppName, string pathToProjectRootDir, string targetFrameworkMoniker, string expectedPathToLaunchFile, Action initiateDebugCallback, IServiceProvider services) : base(services)
        {
            _projectName = expectedAppName;
            _pathToProjectRootDir = pathToProjectRootDir;
            _targetFrameworkMoniker = targetFrameworkMoniker;
            _expectedPathToLaunchFile = expectedPathToLaunchFile;
            _initiateDebugCallback = initiateDebugCallback;
            _tasExplorer = services.GetRequiredService<ITasExplorerViewModel>();
            _cfCliService = services.GetRequiredService<ICfCliService>();
            _dotnetCliService = services.GetRequiredService<IDotnetCliService>();
            _fileService = services.GetRequiredService<IFileService>();

            _vsdbgPathLinux = _vsdbgInstallationDirLinux + "/" + _vsdbgExecutableNameLinux;
            _vsdbgPathWindows = _vsdbgInstallationDirWindows + "\\" + _vsdbgExecutableNameWindows;

            _outputView = ViewLocatorService.GetViewByViewModelName(nameof(OutputViewModel)) as IView;
            _outputViewModel = _outputView?.ViewModel as IOutputViewModel;

            Option1Text = $"Push new version of \"{expectedAppName}\" to debug";
            Option2Text = $"Select existing app to debug";
            AppToDebug = null;
            LoadingMessage = null;

            IsLoggedIn = _tasExplorer != null && _tasExplorer.TasConnection != null;
            if (IsLoggedIn)
            {
                IsLoggedIn = true;
                _cfClient = _tasExplorer.TasConnection.CfClient;
                var _ = BeginRemoteDebuggingAsync(expectedAppName);
            }
        }

        // Properties //

        public Action ViewOpener { get; set; }

        public Action ViewCloser { get; set; }

        public bool WaitingOnAppConfirmation
        {
            get => _waitingOnAppConfirmation;
            set
            {
                _waitingOnAppConfirmation = value;
                RaisePropertyChangedEvent("WaitingOnAppConfirmation");
            }
        }

        public bool IsLoggedIn
        {
            get { return _isLoggedIn; }
            set
            {
                _isLoggedIn = value;
                RaisePropertyChangedEvent("IsLoggedIn");
            }
        }

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

        public List<string> StackOptions
        {
            get => _stackOptions;
            set
            {
                _stackOptions = value;
                RaisePropertyChangedEvent("StackOptions");
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

        public string SelectedStack
        {
            get => _selectedStack;
            set
            {
                _selectedStack = value;
                RaisePropertyChangedEvent("SelectedStack");
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

        public void OpenLoginView(object arg = null)
        {
            _tasExplorer.OpenLoginView(arg);

            if (_tasExplorer != null && _tasExplorer.TasConnection != null)
            {
                IsLoggedIn = true;
                _cfClient = _tasExplorer.TasConnection.CfClient;
                var _ = BeginRemoteDebuggingAsync(_projectName);
            }
        }

        public async Task BeginRemoteDebuggingAsync(string appName)
        {
            await EstablishAppToDebugAsync(appName);
            if (WaitingOnAppConfirmation)
            {
                // resolution delegated to UI;
                // once a decision is made, this method should be called
                // again after WaitingOnAppConfirmation is set to false
                return;
            }

            // sanity check; AppToDebug should always be populated by this step
            if (AppToDebug == null)
            {
                ErrorService.DisplayErrorDialog("Remote Debug Error", "Unable to identify app to debug.\n" +
                    "This is unexpected; it may help to sign out of TAS & try debugging again after logging back in.\n" +
                    "If this issue persists, please contact tas-vs-extension@vmware.com");
                Close();
                return;
            }

            _debugAgentInstalled = await CheckForVsdbg();
            if (!_debugAgentInstalled)
            {
                Option1Text = $"Push new version of \"{AppToDebug.AppName}\" to debug (project \"{_projectName}\")";
                var _ = PromptAppResolutionAsync($"Unable to locate debugging agent on \"{AppToDebug.AppName}\".");
                return;
            }

            CreateLaunchFileIfNonexistent();
            if (!_launchFileExists)
            {
                Close();
                return;
            }

            LoadingMessage = "Attaching to debugging agent...";
            _initiateDebugCallback?.Invoke();
            Close();
            FileService.DeleteFile(_expectedPathToLaunchFile);
        }

        public async Task EstablishAppToDebugAsync(string appName)
        {
            LoadingMessage = "Identifying remote app to debug...";
            await PopulateAccessibleAppsAsync();
            AppToDebug = AccessibleApps.FirstOrDefault(app => app.AppName == appName);
            if (AppToDebug == null)
            {
                WaitingOnAppConfirmation = true;
                await PopulateStackOptionsAsync();
                var _ = PromptAppResolutionAsync($"No app found with a name matching \"{appName}\"");
            }
        }

        public async Task ResolveMissingAppAsync(object arg = null)
        {
            WaitingOnAppConfirmation = false;

            if (PushNewAppToDebug)
            {
                var appName = AppToDebug == null ? _projectName : AppToDebug.AppName;
                Close();
                _outputView.Show();
                await PushNewAppWithDebugConfigurationAsync(appName, SelectedStack);
                var _ = BeginRemoteDebuggingAsync(appName); // start debug process over from beginning
                ViewOpener?.Invoke(); // reopen remote debug dialog
            }
            else if (DebugExistingApp)
            {
                AppToDebug = SelectedApp;
                var _ = BeginRemoteDebuggingAsync(AppToDebug.AppName); // start debug process over from beginning
            }
            else
            {
                var msg = "Encountered unexpected debug strategy";
                Logger.Error(msg);
                ErrorService.DisplayErrorDialog(string.Empty, msg);
                Close();
            }
        }

        private async Task<bool> CheckForVsdbg()
        {
            LoadingMessage = "Checking for debugging agent...";
            bool vsdbExecutableListed;

            // try linux format
            var sshCommand = $"ls {_vsdbgInstallationDirLinux}";
            var sshResult = await _cfCliService.ExecuteSshCommand(AppToDebug.AppName, AppToDebug.ParentSpace.ParentOrg.OrgName, AppToDebug.ParentSpace.SpaceName, sshCommand);
            if (sshResult.CmdResult.StdErr.Contains("'ls' is not recognized"))
            {
                // try windows format
                sshCommand = $"dir {_vsdbgInstallationDirWindows}";
                sshResult = await _cfCliService.ExecuteSshCommand(AppToDebug.AppName, AppToDebug.ParentSpace.ParentOrg.OrgName, AppToDebug.ParentSpace.SpaceName, sshCommand);
                vsdbExecutableListed = sshResult.CmdResult.StdOut.Contains(_vsdbgExecutableNameWindows);
            }
            else
            {
                vsdbExecutableListed = sshResult.CmdResult.StdOut.Contains(_vsdbgExecutableNameLinux);
            }
            return sshResult.Succeeded && vsdbExecutableListed;
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
                        adapter = _fileService.FullPathToCfExe,
                        adapterArgs = $"ssh {AppToDebug.AppName} -c \"/tmp/lifecycle/shell {_appDirLinux} 'bash -c \\\"{_vsdbgPathLinux} --interpreter=vscode\\\"'\"",
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
                                processName = _projectName, // this should be the app name as determined by .NET, not CF,
                                request = "attach",
                                justMyCode = false,
                                cwd = _vsdbgInstallationDirLinux,
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
                ErrorService.DisplayErrorDialog("Unable to attach to remote debugging agent.", $"Failed to specify launch configuration \"{_launchFileName}\".\n" +
                    $"It may help to try disconnecting & signing into TAS again; if this issue persists, please contact tas-vs-extension@vmware.com");
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

        public void Close(object arg = null)
        {
            ViewCloser?.Invoke();
        }

        private async Task PromptAppResolutionAsync(string promptMsg)
        {
            await UpdateCfOrgOptions();
            DialogMessage = promptMsg;
            LoadingMessage = null;
            WaitingOnAppConfirmation = true;
        }

        private async Task PushNewAppWithDebugConfigurationAsync(string appName, string stack)
        {
            var runtimeIdentifier = stack.Contains("win") ? "win-x64" : "linux-x64";
            if (runtimeIdentifier == "linux-x64" && !stack.Contains("linux"))
            {
                Logger.Information($"Unexpected stack provided: '{stack}'; proceeding to publish with default runtime identifier 'linux-x64'...");
            }
            var publishConfiguration = "Debug";
            var publishDirName = "publish";
            try
            {
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
                    var title = "Unable to intitate remote debugging";
                    var msg = "Project failed to publish.";
                    Logger.Error(title + "; " + msg);
                    ErrorService.DisplayErrorDialog(title, msg);
                    _outputViewModel.AppendLine("Project failed to publish");
                    return;
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Caught exception while publishing project for remote debugging: {RemoteDebugPublishException}", ex);
                var title = "Unable to intitate remote debugging";
                var msg = "Project failed to publish.";
                ErrorService.DisplayErrorDialog(title, msg);
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
                        Name = appName,
                        Path = pathToPublishDir,
                        Stack = stack,
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
                var msg = $"Failed to push app '{appName}'; {pushResult.Explanation}";
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

        private async Task PopulateStackOptionsAsync()
        {
            var stacksRespsonse = await _cfClient.GetStackNamesAsync(_tasExplorer.TasConnection.CloudFoundryInstance);
            if (stacksRespsonse.Succeeded)
            {
                StackOptions = stacksRespsonse.Content;
            }
            else
            {
                StackOptions = new List<string>();
                var title = "Unable to retrieve list of available stacks";
                Logger.Error(title + " {StacksResponseError}", stacksRespsonse.Explanation);
                ErrorService.DisplayErrorDialog(title, stacksRespsonse.Explanation);
            }
        }

        // Predicates //

        public bool CanResolveMissingApp(object arg = null)
        {
            return (PushNewAppToDebug && SelectedOrg != null && SelectedSpace != null)
                || (DebugExistingApp && SelectedApp != null);
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
