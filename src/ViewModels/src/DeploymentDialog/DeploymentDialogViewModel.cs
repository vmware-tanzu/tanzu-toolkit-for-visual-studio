using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Tanzu.Toolkit.Models;
using Tanzu.Toolkit.Services;
using Tanzu.Toolkit.Services.DataPersistence;
using Tanzu.Toolkit.Services.DotnetCli;
using Tanzu.Toolkit.Services.ErrorDialog;
using Tanzu.Toolkit.Services.Project;

[assembly: InternalsVisibleTo("Tanzu.Toolkit.ViewModels.Tests")]

namespace Tanzu.Toolkit.ViewModels
{
    public class DeploymentDialogViewModel : AbstractViewModel, IDeploymentDialogViewModel
    {
        internal const string _appNameEmptyMsg = "App name not specified.";
        internal const string _targetEmptyMsg = "Target not specified.";
        internal const string _orgEmptyMsg = "Org not specified.";
        internal const string _spaceEmptyMsg = "Space not specified.";
        internal const string _deploymentSuccessMsg = "App was successfully deployed!\nYou can now close this window.";
        internal const string _deploymentErrorMsg = "Encountered an issue while deploying app:";
        internal const string _getOrgsFailureMsg = "Unable to fetch orgs.";
        internal const string _getSpacesFailureMsg = "Unable to fetch spaces.";
        internal const string _getBuildpacksFailureMsg = "Unable to fetch buildpacks.";
        internal const string _getServicesFailureMsg = "Unable to fetch services.";
        internal const string _getStacksFailureMsg = "Unable to fetch stacks.";
        internal const string _singleLoginErrorTitle = "Unable to add more Tanzu Platform connections.";

        internal const string _singleLoginErrorMessage1 =
            "This version of Tanzu Toolkit for Visual Studio only supports 1 cloud connection at a time; multi-cloud connections will be supported in the future.";

        internal const string _singleLoginErrorMessage2 =
            "If you want to connect to a different cloud, please delete this one by right-clicking on it in the Tanzu Platform Explorer & re-connecting to a new one.";

        internal const string _fullFrameworkTFM = ".NETFramework";
        internal const string _manifestNotFoundTitle = "Unable to set manifest path";
        internal const string _manifestParsingErrorTitle = "Unable to parse app manifest";
        internal const string _directoryNotFoundTitle = "Unable to set push directory path";
        internal const string _publishDirName = "publish";
        internal const int _waitBeforeApplyingManifest = 2000;
        private string _appName;
        internal readonly bool _fullFrameworkDeployment;
        private readonly IErrorDialog _errorDialogService;
        private readonly IDotnetCliService _dotnetCliService;
        internal IView _outputView;
        internal IOutputViewModel _outputViewModel;
        internal ITanzuExplorerViewModel _tanzuExplorerViewModel;
        private readonly IProjectService _projectService;
        private readonly IDataPersistenceService _dataPersistenceService;
        private List<CloudFoundryInstance> _cfInstances;
        private List<CloudFoundryOrganization> _cfOrgs;
        private List<CloudFoundrySpace> _cfSpaces;
        private CloudFoundryOrganization _selectedOrg;
        private CloudFoundrySpace _selectedSpace;
        private string _startCmd;
        private string _manifestPathLabel;
        private string _manifestPath;
        private string _directoryPathLabel;
        private string _directoryPath;
        private string _targetName;
        private bool _isLoggedIn;
        private string _selectedStack;
        private string _serviceNotRecognizedWarningMessage;
        private ObservableCollection<string> _selectedBuildpacks;
        private ObservableCollection<string> _selectedServices;
        private List<string> _stackOptions;
        private List<BuildpackListItem> _buildpackOptions;
        private List<ServiceListItem> _serviceOptions;
        private bool _expanded;
        private string _expansionButtonText;
        private bool _buildpacksLoading;
        private bool _servicesLoading;
        private bool _stacksLoading;
        private bool _publishBeforePushing;
        private bool _configureForRemoteDebugging;
        private readonly string _targetFrameworkMoniker;
        private const int _cfRefreshBuffer = 5000;

        public DeploymentDialogViewModel(IServiceProvider services) : base(services)
        {
            _errorDialogService = services.GetRequiredService<IErrorDialog>();
            _dotnetCliService = services.GetRequiredService<IDotnetCliService>();
            _tanzuExplorerViewModel = services.GetRequiredService<ITanzuExplorerViewModel>();
            _projectService = services.GetRequiredService<IProjectService>();
            _dataPersistenceService = services.GetRequiredService<IDataPersistenceService>();

            _outputView = ViewLocatorService.GetViewByViewModelName(nameof(OutputViewModel), $"Tanzu Push Output (\"{_projectService.ProjectName}\")") as IView;
            _outputViewModel = _outputView?.ViewModel as IOutputViewModel;

            DeploymentInProgress = false;
            PathToProjectRootDir = _projectService.PathToProjectDirectory;
            SelectedBuildpacks = new ObservableCollection<string>();
            SelectedServices = new ObservableCollection<string>();

            _targetFrameworkMoniker = _projectService.TargetFrameworkMoniker;
            if (_targetFrameworkMoniker.StartsWith(_fullFrameworkTFM))
            {
                _fullFrameworkDeployment = true;
            }

            CfInstanceOptions = new List<CloudFoundryInstance>();
            CfOrgOptions = new List<CloudFoundryOrganization>();
            CfSpaceOptions = new List<CloudFoundrySpace>();
            BuildpackOptions = new List<BuildpackListItem>();
            ServiceOptions = new List<ServiceListItem>();
            StackOptions = new List<string>();
            DeploymentDirectoryPath = null;
            ServiceNotRecognizedWarningMessage = null;

            ManifestModel = new AppManifest
            {
                Version = 1,
                Applications = new List<AppConfig> { new AppConfig { Name = _projectService.ProjectName, Buildpacks = new List<string>(), Services = new List<string>() } }
            };

            if (_tanzuExplorerViewModel.CloudFoundryConnection != null)
            {
                TargetName = _tanzuExplorerViewModel.CloudFoundryConnection.DisplayText;
                IsLoggedIn = true;

                ThreadingService.StartBackgroundTask(UpdateCfOrgOptions);
                ThreadingService.StartBackgroundTask(UpdateBuildpackOptions);
                ThreadingService.StartBackgroundTask(UpdateServiceOptions);
                ThreadingService.StartBackgroundTask(UpdateStackOptions);
            }

            // delay calling SetManifestIfDefaultExists to give background update tasks time to complete
            // -> should reduce false-positive "Unrecognized service" complaints
            OnRendered = () => Task.Delay(_waitBeforeApplyingManifest).ContinueWith(_ => SetManifestIfDefaultExists());
            AppName = _projectService.ProjectName;
            Expanded = false;

            OnClose = () =>
            {
                DialogService.CloseDialogByName(nameof(DeploymentDialogViewModel));
                if (DeploymentInProgress) // don't open tool window if modal was closed via "X" button
                {
                    DisplayDeploymentOutput();
                }
            };
        }

        public string AppName
        {
            get => _appName;

            set
            {
                _appName = value;
                RaisePropertyChangedEvent("AppName");

                ManifestModel.Applications[0].Name = value;
            }
        }

        public string StartCommand
        {
            get => _startCmd;

            set
            {
                _startCmd = value;
                RaisePropertyChangedEvent("StartCommand");
                ManifestModel.Applications[0].Command = value;
            }
        }

        public string PathToProjectRootDir { get; }

        public string ManifestPath
        {
            get => _manifestPath;

            set
            {
                if (value == null)
                {
                    _manifestPath = null;
                    ManifestPathLabel = "<none selected>";
                }
                else if (FileService.FileExists(value))
                {
                    _manifestPath = value;

                    ManifestPathLabel = _manifestPath;

                    try
                    {
                        var manifestContents = FileService.ReadFileContents(value);

                        var parsedManifest = SerializationService.ParseCfAppManifest(manifestContents);

                        /** Create 2 AppManifest instances with the same initial data;
                         * the props in this view model should change so that the UI
                         * displays the new incoming manifest data -- those props will
                         * change the ManifestModel so it stays in sync with the state
                         * of this view model. Unintentded side effects arise when using
                         * a single instance of an AppManifest to both read new info from
                         * and to record state on -- deep cloning the initial AppManifest
                         * allows for the new manifest info to stay independent from the
                         * ManifestModel data & unchanged despite any state changes to
                         * the view model.
                         */
                        var modelInstance = parsedManifest.DeepClone();

                        ManifestModel = modelInstance;
                        SetViewModelValuesFromManifest(parsedManifest);
                    }
                    catch (Exception ex)
                    {
                        _errorDialogService.DisplayErrorDialog(_manifestParsingErrorTitle, ex.Message);
                    }
                }
                else
                {
                    _errorDialogService.DisplayWarningDialog(_manifestNotFoundTitle, $"'{value}' does not appear to be a valid path to a manifest.");
                }
            }
        }

        public string ManifestPathLabel
        {
            get => _manifestPathLabel;

            private set
            {
                _manifestPathLabel = value;
                RaisePropertyChangedEvent("ManifestPathLabel");
            }
        }

        public string DeploymentDirectoryPath
        {
            get => _directoryPath;

            set
            {
                if (FileService.DirectoryExists(value) || (value != null && value.EndsWith(_publishDirName)))
                {
                    _directoryPath = value;
                    DirectoryPathLabel = value;

                    ManifestModel.Applications[0].Path = value;
                }
                else
                {
                    if (value != null)
                    {
                        _errorDialogService.DisplayWarningDialog(_directoryNotFoundTitle, $"'{value}' does not appear to be a valid path to a directory.");

                        ManifestModel.Applications[0].Path = null;
                    }

                    _directoryPath = null;
                    DirectoryPathLabel = "<Default App Directory>";
                }
            }
        }

        public string DirectoryPathLabel
        {
            get => _directoryPathLabel;

            internal set
            {
                _directoryPathLabel = value;
                RaisePropertyChangedEvent("DirectoryPathLabel");
            }
        }

        public bool Expanded
        {
            get => _expanded;

            set
            {
                _expanded = value;

                ExpansionButtonText = _expanded ? "Hide Options" : "More Options";

                RaisePropertyChangedEvent("Expanded");
            }
        }

        public string ExpansionButtonText
        {
            get => _expansionButtonText;

            set
            {
                _expansionButtonText = value;
                RaisePropertyChangedEvent("ExpansionButtonText");
            }
        }

        public string SelectedStack
        {
            get => _selectedStack;

            set
            {
                _selectedStack = value;
                RaisePropertyChangedEvent("SelectedStack");

                ManifestModel.Applications[0].Stack = value;

                foreach (var b in BuildpackOptions)
                {
                    b.EvalutateStackCompatibility(value);
                    if (!b.CompatibleWithStack && b.IsSelected)
                    {
                        b.IsSelected = false;
                        RemoveFromSelectedBuildpacks(b.Name);
                    }
                }
            }
        }

        public ObservableCollection<string> SelectedBuildpacks
        {
            get => _selectedBuildpacks;

            set
            {
                _selectedBuildpacks = value;
                RaisePropertyChangedEvent("SelectedBuildpacks");
            }
        }

        public ObservableCollection<string> SelectedServices
        {
            get => _selectedServices;

            set
            {
                _selectedServices = value;
                RaisePropertyChangedEvent("SelectedServices");
            }
        }

        public CloudFoundryOrganization SelectedOrg
        {
            get => _selectedOrg;

            set
            {
                if (value != _selectedOrg)
                {
                    _selectedOrg = value;

                    // clear spaces
                    CfSpaceOptions = new List<CloudFoundrySpace>();
                }

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

        public List<CloudFoundryInstance> CfInstanceOptions
        {
            get => _cfInstances;

            set
            {
                _cfInstances = value;
                RaisePropertyChangedEvent("CfInstanceOptions");
            }
        }

        public List<CloudFoundryOrganization> CfOrgOptions
        {
            get => _cfOrgs;

            set
            {
                _cfOrgs = value;
                RaisePropertyChangedEvent("CfOrgOptions");
            }
        }

        public List<CloudFoundrySpace> CfSpaceOptions
        {
            get => _cfSpaces;

            set
            {
                _cfSpaces = value;
                RaisePropertyChangedEvent("CfSpaceOptions");
            }
        }

        public List<string> StackOptions
        {
            get => _stackOptions;

            internal set
            {
                _stackOptions = value;
                RaisePropertyChangedEvent("StackOptions");
            }
        }

        public List<BuildpackListItem> BuildpackOptions
        {
            get => _buildpackOptions;

            set
            {
                _buildpackOptions = value;

                RaisePropertyChangedEvent("BuildpackOptions");
            }
        }

        public List<ServiceListItem> ServiceOptions
        {
            get => _serviceOptions;

            set
            {
                _serviceOptions = value;

                RaisePropertyChangedEvent("ServiceOptions");
            }
        }

        public bool DeploymentInProgress { get; internal set; }

        public string TargetName
        {
            get => _targetName;

            internal set
            {
                _targetName = value;
                RaisePropertyChangedEvent("TargetName");
            }
        }

        public bool IsLoggedIn
        {
            get => _isLoggedIn;

            set
            {
                _isLoggedIn = value;
                RaisePropertyChangedEvent("IsLoggedIn");
            }
        }

        public AppManifest ManifestModel { get; set; }

        public bool BuildpacksLoading
        {
            get => _buildpacksLoading;

            set
            {
                _buildpacksLoading = value;
                RaisePropertyChangedEvent("BuildpacksLoading");
            }
        }

        public bool ServicesLoading
        {
            get => _servicesLoading;

            set
            {
                _servicesLoading = value;
                RaisePropertyChangedEvent("ServicesLoading");
            }
        }

        public bool StacksLoading
        {
            get => _stacksLoading;

            set
            {
                _stacksLoading = value;
                RaisePropertyChangedEvent("StacksLoading");
            }
        }

        public bool PublishBeforePushing
        {
            get => _publishBeforePushing;

            set
            {
                _publishBeforePushing = value;
                if (_publishBeforePushing)
                {
                    DeploymentDirectoryPath = Path.Combine(PathToProjectRootDir, _publishDirName);
                }
                else
                {
                    ConfigureForRemoteDebugging = false;
                    DeploymentDirectoryPath = null;
                }

                RaisePropertyChangedEvent("PublishBeforePushing");
            }
        }

        public bool ConfigureForRemoteDebugging
        {
            get => _configureForRemoteDebugging;

            set
            {
                _configureForRemoteDebugging = value;
                if (_configureForRemoteDebugging)
                {
                    PublishBeforePushing = true;
                }

                RaisePropertyChangedEvent("ConfigureForRemoteDebugging");
            }
        }

        public string ServiceNotRecognizedWarningMessage
        {
            get => _serviceNotRecognizedWarningMessage;
            set
            {
                _serviceNotRecognizedWarningMessage = value;
                RaisePropertyChangedEvent("ServiceNotRecognizedWarningMessage");
            }
        }

        public Action OnClose { get; set; }

        public Action OnRendered { get; set; }

        public bool CanDeployApp(object arg)
        {
            return !string.IsNullOrEmpty(AppName) && IsLoggedIn && SelectedOrg != null && SelectedSpace != null;
        }

        public void DeployApp(object dialogWindow)
        {
            if (CanDeployApp(null))
            {
                DeploymentInProgress = true;
                var _ = ThreadingService.StartBackgroundTask(StartDeployment);
                DialogService.CloseDialog(dialogWindow, true);
                OnClose();
            }
        }

        public bool CanOpenLoginView(object arg)
        {
            return true;
        }

        public bool CanToggleAdvancedOptions(object arg)
        {
            return true;
        }

        public void OpenLoginView(object arg)
        {
            _tanzuExplorerViewModel.OpenLoginView(arg);

            if (_tanzuExplorerViewModel.CloudFoundryConnection != null)
            {
                CfInstanceOptions = new List<CloudFoundryInstance> { _tanzuExplorerViewModel.CloudFoundryConnection.CloudFoundryInstance };

                TargetName = _tanzuExplorerViewModel.CloudFoundryConnection.DisplayText;
                IsLoggedIn = true;

                ThreadingService.StartBackgroundTask(UpdateCfOrgOptions);
                ThreadingService.StartBackgroundTask(UpdateBuildpackOptions);
                ThreadingService.StartBackgroundTask(UpdateServiceOptions);
                ThreadingService.StartBackgroundTask(UpdateStackOptions);
            }
        }

        private DateTime _lastUpdatedCfOrgOptions = DateTime.Now;

        public async Task UpdateCfOrgOptions()
        {
            if (_tanzuExplorerViewModel.CloudFoundryConnection == null)
            {
                CfOrgOptions = new List<CloudFoundryOrganization>();
            }
            else
            {
                if (CfOrgOptions.Any() && _lastUpdatedCfOrgOptions > DateTime.Now.Subtract(new TimeSpan(_cfRefreshBuffer * 3)))
                {
                    return;
                }

                var orgsResponse =
                    await _tanzuExplorerViewModel.CloudFoundryConnection.CfClient.GetOrgsForCfInstanceAsync(
                        _tanzuExplorerViewModel.CloudFoundryConnection.CloudFoundryInstance);
                if (orgsResponse.Succeeded)
                {
                    CfOrgOptions = orgsResponse.Content;
                    _lastUpdatedCfOrgOptions = DateTime.Now;
                }
                else
                {
                    Logger.Error($"{_getOrgsFailureMsg}. {orgsResponse}");
                    _errorDialogService.DisplayErrorDialog(_getOrgsFailureMsg, orgsResponse.Explanation);
                }
            }
        }

        public async Task UpdateCfSpaceOptions()
        {
            if (SelectedOrg == null || _tanzuExplorerViewModel.CloudFoundryConnection == null)
            {
                CfSpaceOptions = new List<CloudFoundrySpace>();
            }
            else
            {
                var spacesResponse =
                    await _tanzuExplorerViewModel.CloudFoundryConnection.CfClient.GetSpacesForOrgAsync(SelectedOrg);

                if (spacesResponse.Succeeded)
                {
                    CfSpaceOptions = spacesResponse.Content;
                }
                else
                {
                    Logger.Error($"{_getSpacesFailureMsg}. {spacesResponse}");
                    _errorDialogService.DisplayErrorDialog(_getSpacesFailureMsg, spacesResponse.Explanation);
                }
            }
        }

        private DateTime _lastUpdatedBuildpackOptions = DateTime.Now;

        public async Task UpdateBuildpackOptions()
        {
            if (_tanzuExplorerViewModel.CloudFoundryConnection == null)
            {
                BuildpackOptions = new List<BuildpackListItem>();
            }
            else
            {
                if (BuildpackOptions.Any() && _lastUpdatedBuildpackOptions > DateTime.Now.Subtract(new TimeSpan(_cfRefreshBuffer * 3)))
                {
                    return;
                }

                BuildpacksLoading = true;
                var buildpacksResponse =
                    await _tanzuExplorerViewModel.CloudFoundryConnection.CfClient.GetBuildpacksAsync(_tanzuExplorerViewModel.CloudFoundryConnection.CloudFoundryInstance
                        .ApiAddress);

                if (buildpacksResponse.Succeeded)
                {
                    var buildpacks = new List<BuildpackListItem>();

                    foreach (var bp in buildpacksResponse.Content)
                    {
                        var nameSpecifiedInManifest = ManifestModel.Applications[0].Buildpack == bp.Name || ManifestModel.Applications[0].Buildpacks?.Contains(bp.Name) == true;
                        var bpCompatibleWithSelectedStack = SelectedStack == null || SelectedStack == bp.Stack;
                        var nameAlreadyExistsInOptions = buildpacks.Any(b => b.Name == bp.Name);

                        if (nameAlreadyExistsInOptions) // don't add duplicate bp names, just add to list of viable stacks
                        {
                            var existingBp = buildpacks.FirstOrDefault(b => b.Name == bp.Name);

                            if (!existingBp.ValidStacks.Contains(bp.Stack))
                            {
                                existingBp.ValidStacks.Add(bp.Stack);
                            }
                        }
                        else
                        {
                            var newBp = new BuildpackListItem { Name = bp.Name, ValidStacks = new List<string> { bp.Stack }, IsSelected = nameSpecifiedInManifest };

                            newBp.EvalutateStackCompatibility(SelectedStack);

                            buildpacks.Add(newBp);
                        }
                    }

                    BuildpackOptions = buildpacks;
                    BuildpacksLoading = false;
                    _lastUpdatedBuildpackOptions = DateTime.Now;
                }
                else
                {
                    BuildpackOptions = new List<BuildpackListItem>();
                    BuildpacksLoading = false;

                    Logger.Error(_getBuildpacksFailureMsg + " {BuildpacksResponseError}", buildpacksResponse.Explanation);
                    _errorDialogService.DisplayErrorDialog(_getBuildpacksFailureMsg, buildpacksResponse.Explanation);
                }
            }
        }

        private DateTime _lastUpdatedServiceOptions = DateTime.Now;

        public async Task UpdateServiceOptions()
        {
            if (_tanzuExplorerViewModel.CloudFoundryConnection == null)
            {
                if (ServiceOptions == null)
                {
                    ServiceOptions = new List<ServiceListItem>();
                }
            }
            else
            {
                if (ServiceOptions.Any() && _lastUpdatedServiceOptions > DateTime.Now.Subtract(new TimeSpan(_cfRefreshBuffer)))
                {
                    return;
                }

                ServicesLoading = true;
                var servicesResponse =
                    await _tanzuExplorerViewModel.CloudFoundryConnection.CfClient.GetServicesAsync(_tanzuExplorerViewModel.CloudFoundryConnection.CloudFoundryInstance.ApiAddress);

                if (servicesResponse.Succeeded)
                {
                    var serviceListItems = new List<ServiceListItem>();

                    foreach (var sv in servicesResponse.Content)
                    {
                        var nameSpecifiedInManifest = ManifestModel.Applications[0].Services.Contains(sv.Name);
                        var nameAlreadyExistsInOptions = serviceListItems.Any(b => b.Name == sv.Name);

                        if (nameAlreadyExistsInOptions) // don't add duplicate bp names, just add to list of viable stacks
                        {
                            _ = serviceListItems.FirstOrDefault(b => b.Name == sv.Name);
                        }
                        else
                        {
                            var newSv = new ServiceListItem { Name = sv.Name, IsSelected = nameSpecifiedInManifest };

                            serviceListItems.Add(newSv);
                        }
                    }

                    ServiceOptions = serviceListItems;
                    ServicesLoading = false;
                    _lastUpdatedServiceOptions = DateTime.Now;
                }
                else
                {
                    ServiceOptions = new List<ServiceListItem>();
                    ServicesLoading = false;

                    Logger.Error(_getServicesFailureMsg + " {ServicesResponseError}", servicesResponse.Explanation);
                    _errorDialogService.DisplayErrorDialog(_getServicesFailureMsg, servicesResponse.Explanation);
                }
            }
        }

        private DateTime _lastUpdatedStacks = DateTime.Now;

        public async Task UpdateStackOptions()
        {
            if (_tanzuExplorerViewModel.CloudFoundryConnection == null)
            {
                StackOptions = new List<string>();
            }
            else
            {
                if (StackOptions.Any() && _lastUpdatedStacks > DateTime.Now.Subtract(new TimeSpan(_cfRefreshBuffer * 3)))
                {
                    return;
                }

                StacksLoading = true;
                var stacksResponse =
                    await _tanzuExplorerViewModel.CloudFoundryConnection.CfClient.GetStackNamesAsync(_tanzuExplorerViewModel.CloudFoundryConnection.CloudFoundryInstance);

                if (stacksResponse.Succeeded)
                {
                    StackOptions = stacksResponse.Content;
                    StacksLoading = false;
                    _lastUpdatedStacks = DateTime.Now;
                }
                else
                {
                    StackOptions = new List<string>();
                    StacksLoading = false;

                    Logger.Error(_getStacksFailureMsg + " {StacksResponseError}", stacksResponse.Explanation);
                    _errorDialogService.DisplayErrorDialog(_getStacksFailureMsg, stacksResponse.Explanation);
                }
            }
        }

        public void ToggleAdvancedOptions(object arg = null)
        {
            Expanded = !Expanded;
        }

        public void AddToSelectedBuildpacks(object arg)
        {
            if (arg is string buildpackName && !SelectedBuildpacks.Contains(buildpackName))
            {
                SelectedBuildpacks.Add(buildpackName);
                RaisePropertyChangedEvent("SelectedBuildpacks");

                ManifestModel.Applications[0].Buildpacks = SelectedBuildpacks.ToList();
            }
        }

        public void RemoveFromSelectedBuildpacks(object arg)
        {
            if (arg is string buildpackName)
            {
                SelectedBuildpacks.Remove(buildpackName);
                RaisePropertyChangedEvent("SelectedBuildpacks");

                ManifestModel.Applications[0].Buildpacks = SelectedBuildpacks.ToList();
            }
        }

        public void ClearSelectedBuildpacks(object arg = null)
        {
            SelectedBuildpacks.Clear();

            foreach (var bpItem in BuildpackOptions)
            {
                bpItem.IsSelected = false;
            }

            RaisePropertyChangedEvent("SelectedBuildpacks");
        }

        public void ClearSelectedManifest(object arg = null)
        {
            ManifestPath = null;
            ResetAllPushConfigValues();
        }

        public void ClearSelectedDeploymentDirectory(object arg = null)
        {
            DeploymentDirectoryPath = null;
        }

        public void AddToSelectedServices(object arg)
        {
            if (!(arg is string serviceName) || SelectedServices.Contains(serviceName))
            {
                return;
            }

            SelectedServices.Add(serviceName);
            RaisePropertyChangedEvent("SelectedServices");

            ManifestModel.Applications[0].Services = SelectedServices.ToList();
        }

        public void RemoveFromSelectedServices(object arg)
        {
            if (arg is string serviceName)
            {
                SelectedServices.Remove(serviceName);
                RaisePropertyChangedEvent("SelectedServices");

                ManifestModel.Applications[0].Services = SelectedServices.ToList();
                RemoveWarningIfAllSelectedServicesExist();
            }
        }

        public void ClearSelectedServices(object arg = null)
        {
            foreach (var svcName in SelectedServices.ToList()) // copy to avoid iterating over a collection that's being modified
            {
                RemoveFromSelectedServices(svcName);
                var itemInOptions = ServiceOptions.FirstOrDefault(item => item.Name == svcName);
                if (itemInOptions != null)
                {
                    itemInOptions.IsSelected = false;
                }
            }

            RemoveWarningIfAllSelectedServicesExist();
            RaisePropertyChangedEvent("SelectedServices");
        }

        public void WriteManifestToFile(string path)
        {
            try
            {
                var manifestContents = SerializationService.SerializeCfAppManifest(ManifestModel);

                FileService.WriteTextToFile(path, manifestContents);
            }
            catch (Exception ex)
            {
                var errorMsg = $"Encountered an error while writing manifest contents to new file {path} : {ex.Message}";

                Logger.Error(errorMsg + ex.StackTrace);

                _errorDialogService.DisplayErrorDialog("Unable to save manifest file", errorMsg);
            }
        }

        internal async Task StartDeployment()
        {
            if (PublishBeforePushing)
            {
                var runtimeIdentifier = "linux-x64";
                if (SelectedStack != null && SelectedStack.Contains("windows"))
                {
                    runtimeIdentifier = "win-x64";
                }

                var publishConfiguration = ConfigureForRemoteDebugging ? "Debug" : "Release";
                var publishSucceeded = await _dotnetCliService.PublishProjectForRemoteDebuggingAsync(
                    PathToProjectRootDir,
                    _targetFrameworkMoniker,
                    runtimeIdentifier,
                    publishConfiguration,
                    _publishDirName,
                    StdOutCallback: _outputViewModel.AppendLine,
                    StdErrCallback: _outputViewModel.AppendLine);

                if (!publishSucceeded)
                {
                    _errorDialogService.DisplayErrorDialog("Unable to publish project with these parameters:\n",
                        $"Project path: {PathToProjectRootDir}\n" +
                        $"Target framework: {_targetFrameworkMoniker}\n" +
                        $"Runtime: {runtimeIdentifier}\n" +
                        $"Configuration: {publishConfiguration}\n" +
                        $"Output directory: {_publishDirName}");
                    return;
                }

                if (ConfigureForRemoteDebugging)
                {
                    var vsdbgPathSource = runtimeIdentifier.Contains("win")
                        ? nameof(IDEOptions.VsdbgWindowsPath)
                        : nameof(IDEOptions.VsdbgLinuxPath);
                    var vsdbgPath = _dataPersistenceService.ReadStringData(vsdbgPathSource);
                    if (!string.IsNullOrEmpty(vsdbgPath) && Directory.Exists(vsdbgPath))
                    {
                        _outputViewModel.AppendLine($"VsdbgPath is set to '{vsdbgPath}' in Visual Studio Options. Copying to publish output...");

                        var destinationPath = Path.Combine(PathToProjectRootDir, _publishDirName, "vsdbg");

                        // It looks weird now, but the script approach installs vsdbg at "app/vsdbg/vsdbg", so try to duplicate that here 
                        if (File.Exists(Path.Combine(vsdbgPath, "vsdbg.dll")) || File.Exists(Path.Combine(vsdbgPath, "vsdbg.managed.dll")))
                        {
                            destinationPath = Path.Combine(destinationPath, "vsdbg");
                        }

                        CopyDirectory(vsdbgPath, destinationPath, true);
                        _outputViewModel.AppendLine("Finished copying vsdbg!");
                    }
                }
            }

            try
            {
                var manifestContents = SerializationService.SerializeCfAppManifest(ManifestModel);
                _outputViewModel.AppendLine($"Pushing app with this configuration:\n{manifestContents}");
            }
            catch (Exception ex)
            {
                Logger?.Error("Unable to serialize manifest contents: {AppConfig}. {SerializationException}", ManifestModel, ex);
            }

            var deploymentResult = await _tanzuExplorerViewModel.CloudFoundryConnection.CfClient.DeployAppAsync(
                ManifestModel,
                PathToProjectRootDir,
                SelectedSpace.ParentOrg.ParentCf,
                SelectedSpace.ParentOrg,
                SelectedSpace,
                stdOutCallback: _outputViewModel.AppendLine,
                stdErrCallback: _outputViewModel.AppendLine);

            if (!deploymentResult.Succeeded)
            {
                if (deploymentResult.FailureType == FailureType.InvalidRefreshToken)
                {
                    _tanzuExplorerViewModel.AuthenticationRequired = true;
                }

                var errorTitle = $"{_deploymentErrorMsg} {AppName}.";
                var errorMsg = deploymentResult.Explanation.Replace("Instances starting...\n", "");

                Logger.Error(
                    "DeploymentDialogViewModel initiated app deployment of {AppName} to target {TargetApi}.{TargetOrg}.{TargetSpace}; deployment result reported failure: {DplmtResult}.",
                    AppName,
                    SelectedSpace.ParentOrg.ParentCf.ApiAddress,
                    SelectedSpace.ParentOrg.OrgName,
                    SelectedSpace.SpaceName,
                    deploymentResult.ToString());

                _errorDialogService.DisplayErrorDialog(errorTitle, errorMsg);
            }

            DeploymentInProgress = false;
        }

        // https://learn.microsoft.com/en-us/dotnet/standard/io/how-to-copy-directories#example
        private static void CopyDirectory(string sourceDir, string destinationDir, bool recursive)
        {
            // Get information about the source directory
            var dir = new DirectoryInfo(sourceDir);

            // Check if the source directory exists
            if (!dir.Exists)
                throw new DirectoryNotFoundException($"Source directory not found: {dir.FullName}");

            // Cache directories before we start copying
            var dirs = dir.GetDirectories();

            // Create the destination directory
            Directory.CreateDirectory(destinationDir);

            // Get the files in the source directory and copy to the destination directory
            foreach (var file in dir.GetFiles())
            {
                var targetFilePath = Path.Combine(destinationDir, file.Name);
                file.CopyTo(targetFilePath);
            }

            // If recursive and copying subdirectories, recursively call this method
            if (recursive)
            {
                foreach (var subDir in dirs)
                {
                    var newDestinationDir = Path.Combine(destinationDir, subDir.Name);
                    CopyDirectory(subDir.FullName, newDestinationDir, true);
                }
            }
        }

        private void SetManifestIfDefaultExists()
        {
            var expectedManifestLocation1 = Path.Combine(PathToProjectRootDir, "manifest.yaml");
            var expectedManifestLocation2 = Path.Combine(PathToProjectRootDir, "manifest.yml");

            if (FileService.FileExists(expectedManifestLocation1))
            {
                ManifestPath = expectedManifestLocation1;
            }
            else if (FileService.FileExists(expectedManifestLocation2))
            {
                ManifestPath = expectedManifestLocation2;
            }
            else
            {
                ManifestPath = null;
            }
        }

        private void SetViewModelValuesFromManifest(AppManifest manifest)
        {
            ResetAllPushConfigValues();

            SetAppNameFromManifest(manifest);
            SetStackFromManifest(manifest);
            SetBuildpacksFromManifest(manifest);
            SetServicesFromManifest(manifest);
            SetStartCommandFromManifest(manifest);
            SetPathFromManifest(manifest);
        }

        private void ResetAllPushConfigValues()
        {
            AppName = _projectService.ProjectName;
            SelectedStack = null;
            ClearSelectedBuildpacks();
            ClearSelectedServices();
            StartCommand = null;
            DeploymentDirectoryPath = null;
        }

        private void SetAppNameFromManifest(AppManifest appManifest)
        {
            var appName = appManifest.Applications[0].Name;
            if (!string.IsNullOrWhiteSpace(appName))
            {
                AppName = appName;
            }
        }

        private void SetStackFromManifest(AppManifest appManifest)
        {
            SelectedStack = appManifest.Applications[0].Stack;
        }

        private void SetBuildpacksFromManifest(AppManifest appManifest)
        {
            var appConfig = appManifest.Applications[0];

            var bps = appConfig.Buildpacks;
            var stack = appConfig.Stack;

            if (bps != null)
            {
                foreach (var bpName in bps)
                {
                    AddToSelectedBuildpacks(bpName);

                    // mark corresponding buildpack option as selected
                    var existingBpOption = BuildpackOptions.FirstOrDefault(b => b.Name == bpName);
                    if (existingBpOption != null)
                    {
                        existingBpOption.IsSelected = true;
                        existingBpOption.EvalutateStackCompatibility(stack);
                    }
                }
            }
        }

        private void SetServicesFromManifest(AppManifest appManifest)
        {
            var appConfig = appManifest.Applications[0];

            var svs = appConfig.Services;

            if (svs == null)
            {
                return;
            }

            var unrecognizedSvcNames = new List<string>();

            foreach (var svName in svs)
            {
                AddToSelectedServices(svName);

                // mark corresponding service option as selected
                var existingSvOption = ServiceOptions.FirstOrDefault(b => b.Name == svName);
                if (existingSvOption != null)
                {
                    existingSvOption.IsSelected = true;
                }

                var svcPresentInOptions = ServiceOptions.Exists(s => s.Name == svName);
                if (!svcPresentInOptions)
                {
                    ApplyUnrecognizedServiceWarning(svName);
                    unrecognizedSvcNames.Add(svName);
                }
            }

            if (unrecognizedSvcNames.Count > 0)
            {
                var svcStr = unrecognizedSvcNames.Aggregate("", (current, svcName) => current + $"{Environment.NewLine}    - {svcName}");
                ErrorService.DisplayWarningDialog(
                    "Unrecognized service provided",
                    "Manifest indicated that the following should be used, but no such service detected:" +
                    Environment.NewLine + svcStr + Environment.NewLine + Environment.NewLine +
                    "Deployment may not succeed.");
            }
        }

        private void ApplyUnrecognizedServiceWarning(string svName)
        {
            ServiceNotRecognizedWarningMessage = string.IsNullOrWhiteSpace(ServiceNotRecognizedWarningMessage)
                ? $"'{svName}' not recognized"
                : "Multiple selected services not recognized";
        }

        private void RemoveWarningIfAllSelectedServicesExist()
        {
            if (SelectedServices.All(remainingSvcName => ServiceOptions.Exists(item => item.Name == remainingSvcName)))
            {
                ServiceNotRecognizedWarningMessage = null;
            }
        }

        private void SetStartCommandFromManifest(AppManifest appManifest)
        {
            var startCmd = appManifest.Applications[0].Command;

            StartCommand = string.IsNullOrWhiteSpace(startCmd) ? null : startCmd;
        }

        private void SetPathFromManifest(AppManifest appManifest)
        {
            var path = appManifest.Applications[0].Path;

            DeploymentDirectoryPath = string.IsNullOrWhiteSpace(path) ? null : path;
        }

        private void DisplayDeploymentOutput()
        {
            _outputView.DisplayView();
        }
    }

    public class BuildpackListItem : INotifyPropertyChanged
    {
        private bool _isSelected;
        private bool _compatibleWithStack;

        public event PropertyChangedEventHandler PropertyChanged;

        public string Name { get; set; }

        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                _isSelected = value;
                RaisePropertyChangedEvent("IsSelected");
            }
        }

        public bool CompatibleWithStack
        {
            get => _compatibleWithStack;

            private set
            {
                _compatibleWithStack = value;

                RaisePropertyChangedEvent("CompatibleWithStack");
            }
        }

        public List<string> ValidStacks { get; set; }

        public void EvalutateStackCompatibility(string stackName)
        {
            CompatibleWithStack = ValidStacks.Contains(stackName) || stackName == null;
        }

        protected void RaisePropertyChangedEvent(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class ServiceListItem : INotifyPropertyChanged
    {
        private bool _isSelected;

        public event PropertyChangedEventHandler PropertyChanged;

        public string Name { get; set; }

        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                _isSelected = value;
                RaisePropertyChangedEvent("IsSelected");
            }
        }

        protected void RaisePropertyChangedEvent(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}