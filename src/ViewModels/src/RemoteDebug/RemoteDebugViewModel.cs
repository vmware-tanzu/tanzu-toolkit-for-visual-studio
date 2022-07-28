using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Tanzu.Toolkit.Models;
using Tanzu.Toolkit.Services;
using Tanzu.Toolkit.Services.CfCli;
using Tanzu.Toolkit.Services.CloudFoundry;
using Tanzu.Toolkit.Services.DebugAgentProvider;
using Tanzu.Toolkit.Services.DotnetCli;
using Tanzu.Toolkit.Services.File;
using Tanzu.Toolkit.Services.Project;

namespace Tanzu.Toolkit.ViewModels.RemoteDebug
{
    public class RemoteDebugViewModel : AbstractViewModel, IRemoteDebugViewModel
    {
        private readonly ITasExplorerViewModel _tasExplorer;
        private ICloudFoundryService _cfClient;
        private readonly ICfCliService _cfCliService;
        private readonly IDotnetCliService _dotnetCliService;
        private readonly IFileService _fileService;
        private readonly ISerializationService _serializationService;
        private readonly IDebugAgentProvider _vsdbgInstaller;
        private readonly IView _outputView;
        private readonly IOutputViewModel _outputViewModel;
        private readonly string _projectName;
        private readonly string _pathToProjectRootDir;
        private readonly string _targetFrameworkMoniker;
        private readonly string _expectedPathToLaunchFile;
        private readonly Action<string, string> _initiateDebugCallback;
        private List<CloudFoundryApp> _accessibleApps;
        private string _dialogMessage;
        private string _loadingMessage;
        private CloudFoundryApp _appToDebug;
        private bool _debugExistingApp;
        private bool _pushNewAppToDebug;
        private bool _isLoggedIn;
        private string _option1Text;
        private string _option2Text;
        private CloudFoundryApp _selectedApp;
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

        public RemoteDebugViewModel(string expectedAppName, string pathToProjectRootDir, string targetFrameworkMoniker, string expectedPathToLaunchFile, Action<string, string> initiateDebugCallback, IServiceProvider services) : base(services)
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
            _serializationService = services.GetRequiredService<ISerializationService>();
            _vsdbgInstaller = services.GetRequiredService<IDebugAgentProvider>();

            _vsdbgPathLinux = _vsdbgInstallationDirLinux + "/" + _vsdbgExecutableNameLinux;
            _vsdbgPathWindows = _vsdbgInstallationDirWindows + "\\" + _vsdbgExecutableNameWindows;

            _outputView = ViewLocatorService.GetViewByViewModelName(nameof(OutputViewModel), $"Remote Debug Output (\"{_projectName}\")") as IView;
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

        public string LoadingMessage
        {
            get => _loadingMessage;

            set
            {
                _loadingMessage = value;
                RaisePropertyChangedEvent("LoadingMessage");
            }
        }

        public string PushNewAppButtonText
        {
            get => "Push New App to Debug";
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
            LoadingMessage = "Fetching apps...";
            await PopulateAccessibleAppsAsync();

            PromptAppResolution();

            // sanity check; AppToDebug should always be populated by this step
            if (AppToDebug == null)
            {
                ErrorService.DisplayErrorDialog("Remote Debug Error", "Unable to identify app to debug.\n" +
                    "This is unexpected; it may help to sign out of TAS & try debugging again after logging back in.\n" +
                    "If this issue persists, please contact tas-vs-extension@vmware.com");
                Close();
                return;
            }
            
            LoadingMessage = $"Checking for debugging agent on {AppToDebug.AppName}...";

            _debugAgentInstalled = await CheckForVsdbg(AppToDebug.Stack);
            if (!_debugAgentInstalled)
            {
                LoadingMessage = $"Installing debugging agent for {AppToDebug.AppName}...";
                var installationResult = await _vsdbgInstaller.InstallVsdbgForCFAppAsync(AppToDebug);
                _debugAgentInstalled = await CheckForVsdbg(AppToDebug.Stack);
                if (!_debugAgentInstalled)
                {
                    Logger.Error("Failed to install or start debugging agent for app '{AppName}': {DebugFailureMsg}", AppToDebug.AppName, installationResult.Explanation);
                }
            }

            CreateLaunchFileIfNonexistent(AppToDebug.Stack);
            if (!_launchFileExists)
            {
                Close();
                return;
            }

            LoadingMessage = "Attaching to debugging agent...";
            _initiateDebugCallback?.Invoke(AppToDebug.ParentSpace.ParentOrg.OrgName, AppToDebug.ParentSpace.SpaceName);
            Close();
            FileService.DeleteFile(_expectedPathToLaunchFile);
        }

        public async Task StartDebuggingAppAsync(object arg = null)
        {
            AppToDebug = SelectedApp;
            var _ = BeginRemoteDebuggingAsync(AppToDebug.AppName); // start debug process over from beginning
        }

        private async Task<bool> CheckForVsdbg(string stack)
        {
            DetailedResult sshResult;
            bool vsdbExecutableListed;

            if (stack.Contains("win"))
            {
                var sshCommand = $"dir {_vsdbgInstallationDirWindows}";
                sshResult = await _cfCliService.ExecuteSshCommand(AppToDebug.AppName, AppToDebug.ParentSpace.ParentOrg.OrgName, AppToDebug.ParentSpace.SpaceName, sshCommand);
                vsdbExecutableListed = sshResult.CmdResult.StdOut.Contains(_vsdbgExecutableNameWindows);
            }
            else
            {
                var sshCommand = $"ls {_vsdbgInstallationDirLinux}";
                sshResult = await _cfCliService.ExecuteSshCommand(AppToDebug.AppName, AppToDebug.ParentSpace.ParentOrg.OrgName, AppToDebug.ParentSpace.SpaceName, sshCommand);
                vsdbExecutableListed = sshResult.CmdResult.StdOut.Contains(_vsdbgExecutableNameLinux);
            }

            return sshResult.Succeeded && vsdbExecutableListed;
        }

        public void CreateLaunchFileIfNonexistent(string stack)
        {
            _launchFileExists = false;
            var remoteDebugAgentDir = stack.Contains("win") ? _vsdbgInstallationDirWindows : _vsdbgInstallationDirLinux;

            try
            {
                if (!File.Exists(_expectedPathToLaunchFile))
                {
                    var appProcessName = _projectName; // this should be the app name as determined by .NET, not CF,
                    if (stack.Contains("win"))
                    {
                        if (_targetFrameworkMoniker.StartsWith(".NETFramework"))
                        {
                            appProcessName = "hwc.exe";
                        }
                        else
                        {
                            appProcessName += ".exe";
                        }
                    }

                    var sshCmd = stack.Contains("win")
                        ? $"{_vsdbgPathWindows}"
                        : $"/tmp/lifecycle/shell {_appDirLinux} {_vsdbgPathLinux}";

                    var launchFileConfig = new RemoteDebugLaunchConfig
                    {
                        version = "0.2.0",
                        adapter = FileService.PathToCfDebugAdapter,
                        adapterArgs = $"\"{FileService.VsixPackageBaseDir}\" \"{FileService.FullPathToCfExe}\" \"{AppToDebug.AppName}\" \"{sshCmd}\"",
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
                                processName = appProcessName,
                                request = "attach",
                                justMyCode = false,
                                cwd = remoteDebugAgentDir,
                                logging = new Logging
                                {
                                    engineLogging = true,
                                },
                            },
                        }
                    };
                    var newLaunchFileContents = JsonSerializer.Serialize(launchFileConfig, _serializationOptions);
                    Logger.Information("About to try attaching to remote debugger with this configuration: {RemoteDebugConfig}", newLaunchFileContents);
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

        public void Close(object arg = null)
        {
            ViewCloser?.Invoke();
        }

        public void DisplayDeploymentWindow(object arg = null)
        {
            var projSvc = Services.GetRequiredService<IProjectService>();
            projSvc.ProjectName = _projectName;
            projSvc.PathToProjectDirectory = _pathToProjectRootDir;
            projSvc.TargetFrameworkMoniker = _targetFrameworkMoniker;

            var view = ViewLocatorService.GetViewByViewModelName(nameof(DeploymentDialogViewModel)) as IView;
            var deploymentViewModel = view.ViewModel as IDeploymentDialogViewModel;
            if (view.ViewModel is IDeploymentDialogViewModel vm)
            {
                vm.ConfigureForRemoteDebugging = true;
                vm.OnClosed += () => Close();
                view.DisplayView();
            }
        }

        private void PromptAppResolution()
        {
            LoadingMessage = null;
            DialogMessage = "Select app to debug:";
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

        public bool CanStartDebuggingApp(object arg = null)
        {
            return SelectedApp != null;
        }

        public bool CanDisplayDeploymentWindow(object arg = null)
        {
            return IsLoggedIn && string.IsNullOrWhiteSpace(LoadingMessage);
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
