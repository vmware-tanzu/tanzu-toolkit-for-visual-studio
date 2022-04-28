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
        internal const string _singleLoginErrorTitle = "Unable to add more TAS connections.";
        internal const string _singleLoginErrorMessage1 = "This version of Tanzu Toolkit for Visual Studio only supports 1 cloud connection at a time; multi-cloud connections will be supported in the future.";
        internal const string _singleLoginErrorMessage2 = "If you want to connect to a different cloud, please delete this one by right-clicking on it in the Tanzu Application Service Explorer & re-connecting to a new one.";
        internal const string _fullFrameworkTFM = ".NETFramework";
        internal const string _manifestNotFoundTitle = "Unable to set manifest path";
        internal const string _manifestParsingErrorTitle = "Unable to parse app manifest";
        internal const string _directoryNotFoundTitle = "Unable to set push directory path";
        internal const string _publishDirName = "publish";
        private string _appName;
        internal readonly bool _fullFrameworkDeployment = false;
        private readonly IErrorDialog _errorDialogService;
        private readonly IDotnetCliService _dotnetCliService;
        internal IOutputViewModel _outputViewModel;
        internal ITasExplorerViewModel _tasExplorerViewModel;
        private readonly IProjectService _projectService;
        private List<CloudFoundryInstance> _cfInstances;
        private List<CloudFoundryOrganization> _cfOrgs;
        private List<CloudFoundrySpace> _cfSpaces;
        private CloudFoundryOrganization _selectedOrg;
        private CloudFoundrySpace _selectedSpace;
        private string _startCmmd;
        private readonly string _projectName;
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
        private AppManifest _appManifest;
        private bool _buildpacksLoading = false;
        private bool _servicesLoading = false;
        private bool _stacksLoading = false;
        private bool _publishBeforePushing;
        private bool _configureForRemoteDebugging;
        private readonly string _targetFrameworkMoniker;

        public DeploymentDialogViewModel(IServiceProvider services) : base(services)
        {
            _errorDialogService = services.GetRequiredService<IErrorDialog>();
            _dotnetCliService = services.GetRequiredService<IDotnetCliService>();
            _tasExplorerViewModel = services.GetRequiredService<ITasExplorerViewModel>();
            _projectService = services.GetRequiredService<IProjectService>();

            OutputView = ViewLocatorService.GetViewByViewModelName(nameof(OutputViewModel), $"Tanzu Push Output (\"{_projectService.ProjectName}\")") as IView;
            _outputViewModel = OutputView?.ViewModel as IOutputViewModel;

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
                Applications = new List<AppConfig>
                {
                    new AppConfig
                    {
                        Name = _projectService.ProjectName,
                        Buildpacks = new List<string>(),
                        Services = new List<string>(),
                    }
                }
            };

            SetManifestIfDefaultExists();

            if (_tasExplorerViewModel.TasConnection != null)
            {
                TargetName = _tasExplorerViewModel.TasConnection.DisplayText;
                IsLoggedIn = true;

                ThreadingService.StartBackgroundTask(UpdateCfOrgOptions);
                ThreadingService.StartBackgroundTask(UpdateBuildpackOptions);
                ThreadingService.StartBackgroundTask(UpdateServiceOptions);
                ThreadingService.StartBackgroundTask(UpdateStackOptions);
            }

            AppName = _projectService.ProjectName;
            _projectName = _projectService.ProjectName;
            Expanded = false;
        }

        public IView OutputView { get; internal set; }

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
            get => _startCmmd;

            set
            {
                _startCmmd = value;
                RaisePropertyChangedEvent("StartCommand");
                ManifestModel.Applications[0].Command = value;
            }
        }

        public string PathToProjectRootDir { get; private set; }

        public string ManifestPath
        {
            get => _manifestPath;

            set
            {
                if (value == null)
                {
                    _manifestPath = value;
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
                if (value != _selectedSpace)
                {
                    _selectedSpace = value;
                }

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

            internal set
            {
                _isLoggedIn = value;
                RaisePropertyChangedEvent("IsLoggedIn");
            }
        }

        public AppManifest ManifestModel
        {
            get => _appManifest;
            set => _appManifest = value;
        }

        public bool BuildpacksLoading
        {
            get { return _buildpacksLoading; }

            set
            {
                _buildpacksLoading = value;
                RaisePropertyChangedEvent("BuildpacksLoading");
            }
        }

        public bool ServicesLoading
        {
            get { return _servicesLoading; }

            set
            {
                _servicesLoading = value;
                RaisePropertyChangedEvent("ServicesLoading");
            }
        }

        public bool StacksLoading
        {
            get { return _stacksLoading; }

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
            _tasExplorerViewModel.OpenLoginView(arg);

            if (_tasExplorerViewModel.TasConnection != null)
            {
                CfInstanceOptions = new List<CloudFoundryInstance>
                {
                    _tasExplorerViewModel.TasConnection.CloudFoundryInstance,
                };

                TargetName = _tasExplorerViewModel.TasConnection.DisplayText;
                IsLoggedIn = true;

                ThreadingService.StartBackgroundTask(UpdateCfOrgOptions);
                ThreadingService.StartBackgroundTask(UpdateBuildpackOptions);
                ThreadingService.StartBackgroundTask(UpdateServiceOptions);
                ThreadingService.StartBackgroundTask(UpdateStackOptions);
            }
        }

        public async Task UpdateCfOrgOptions()
        {
            if (_tasExplorerViewModel.TasConnection == null)
            {
                CfOrgOptions = new List<CloudFoundryOrganization>();
            }
            else
            {
                var orgsResponse = await _tasExplorerViewModel.TasConnection.CfClient.GetOrgsForCfInstanceAsync(_tasExplorerViewModel.TasConnection.CloudFoundryInstance);
                if (orgsResponse.Succeeded)
                {
                    CfOrgOptions = orgsResponse.Content;
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
            if (SelectedOrg == null || _tasExplorerViewModel.TasConnection == null)
            {
                CfSpaceOptions = new List<CloudFoundrySpace>();
            }
            else
            {
                var spacesResponse = await _tasExplorerViewModel.TasConnection.CfClient.GetSpacesForOrgAsync(SelectedOrg);

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

        public async Task UpdateBuildpackOptions()
        {
            if (_tasExplorerViewModel.TasConnection == null)
            {
                BuildpackOptions = new List<BuildpackListItem>();
            }
            else
            {
                BuildpacksLoading = true;
                var buildpacksRespsonse = await _tasExplorerViewModel.TasConnection.CfClient.GetBuildpacksAsync(_tasExplorerViewModel.TasConnection.CloudFoundryInstance.ApiAddress);

                if (buildpacksRespsonse.Succeeded)
                {
                    var bpOtps = new List<BuildpackListItem>();

                    foreach (var bp in buildpacksRespsonse.Content)
                    {
                        var nameSpecifiedInManifest = ManifestModel.Applications[0].Buildpacks.Contains(bp.Name);
                        var bpCompatibleWithSelectedStack = SelectedStack == null || SelectedStack == bp.Stack;
                        var nameAlreadyExistsInOptions = bpOtps.Any(b => b.Name == bp.Name);

                        if (nameAlreadyExistsInOptions) // don't add duplicate bp names, just add to list of viable stacks
                        {
                            var existingBp = bpOtps.FirstOrDefault(b => b.Name == bp.Name);

                            if (!existingBp.ValidStacks.Contains(bp.Stack))
                            {
                                existingBp.ValidStacks.Add(bp.Stack);
                            }
                        }
                        else
                        {
                            var newBp = new BuildpackListItem
                            {
                                Name = bp.Name,
                                ValidStacks = new List<string> { bp.Stack },
                                IsSelected = nameSpecifiedInManifest,
                            };

                            newBp.EvalutateStackCompatibility(SelectedStack);

                            bpOtps.Add(newBp);
                        }
                    }

                    BuildpackOptions = bpOtps;
                    BuildpacksLoading = false;
                }
                else
                {
                    BuildpackOptions = new List<BuildpackListItem>();
                    BuildpacksLoading = false;

                    Logger.Error(_getBuildpacksFailureMsg + " {BuildpacksResponseError}", buildpacksRespsonse.Explanation);
                    _errorDialogService.DisplayErrorDialog(_getBuildpacksFailureMsg, buildpacksRespsonse.Explanation);
                }
            }
        }

        public async Task UpdateServiceOptions()
        {
            if (_tasExplorerViewModel.TasConnection == null)
            {
                ServiceOptions = new List<ServiceListItem>();
            }
            else
            {
                ServicesLoading = true;
                var servicesRespsonse = await _tasExplorerViewModel.TasConnection.CfClient.GetServicesAsync(_tasExplorerViewModel.TasConnection.CloudFoundryInstance.ApiAddress);

                if (servicesRespsonse.Succeeded)
                {
                    var svOtps = new List<ServiceListItem>();

                    foreach (var sv in servicesRespsonse.Content)
                    {
                        var nameSpecifiedInManifest = ManifestModel.Applications[0].Services.Contains(sv.Name);
                        var nameAlreadyExistsInOptions = svOtps.Any(b => b.Name == sv.Name);

                        if (nameAlreadyExistsInOptions) // don't add duplicate bp names, just add to list of viable stacks
                        {
                            var existingSv = svOtps.FirstOrDefault(b => b.Name == sv.Name);
                        }
                        else
                        {
                            var newSv = new ServiceListItem
                            {
                                Name = sv.Name,
                                IsSelected = nameSpecifiedInManifest,
                            };

                            svOtps.Add(newSv);
                        }
                    }

                    ServiceOptions = svOtps;
                    ServicesLoading = false;
                }
                else
                {
                    ServiceOptions = new List<ServiceListItem>();
                    ServicesLoading = false;

                    Logger.Error(_getServicesFailureMsg + " {ServicesResponseError}", servicesRespsonse.Explanation);
                    _errorDialogService.DisplayErrorDialog(_getServicesFailureMsg, servicesRespsonse.Explanation);
                }
            }
        }

        public async Task UpdateStackOptions()
        {
            if (_tasExplorerViewModel.TasConnection == null)
            {
                StackOptions = new List<string>();
            }
            else
            {
                StacksLoading = true;
                var stacksRespsonse = await _tasExplorerViewModel.TasConnection.CfClient.GetStackNamesAsync(_tasExplorerViewModel.TasConnection.CloudFoundryInstance);

                if (stacksRespsonse.Succeeded)
                {
                    StackOptions = stacksRespsonse.Content;
                    StacksLoading = false;
                }
                else
                {
                    StackOptions = new List<string>();
                    StacksLoading = false;

                    Logger.Error(_getStacksFailureMsg + " {StacksResponseError}", stacksRespsonse.Explanation);
                    _errorDialogService.DisplayErrorDialog(_getStacksFailureMsg, stacksRespsonse.Explanation);
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
        }

        public void AddToSelectedServices(object arg)
        {
            if (arg is string serviceName && !SelectedServices.Contains(serviceName))
            {
                SelectedServices.Add(serviceName);
                RaisePropertyChangedEvent("SelectedServices");

                ManifestModel.Applications[0].Services = SelectedServices.ToList();
            }
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
            SelectedServices.Clear();
            foreach (var svItem in ServiceOptions)
            {
                svItem.IsSelected = false;
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
                    includeDebuggingAgent: ConfigureForRemoteDebugging,
                    StdOutCallback: _outputViewModel.AppendLine,
                    StdErrCallback: _outputViewModel.AppendLine);

                if (!publishSucceeded)
                {
                    _errorDialogService.DisplayErrorDialog("Unable to publish project with these parameters:\n",
                        $"Project path: {PathToProjectRootDir}\n" +
                        $"Target framework: {_targetFrameworkMoniker}\n" +
                        $"Runtime: {runtimeIdentifier}\n" +
                        $"Configuration: {publishConfiguration}\n" +
                        $"Output directory: {_publishDirName}\n" +
                        $"\nIf this issue persists, please contact tas-vs-extension@vmware.com");
                    return;
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

            var deploymentResult = await _tasExplorerViewModel.TasConnection.CfClient.DeployAppAsync(
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
                    _tasExplorerViewModel.AuthenticationRequired = true;
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
            SetAppNameFromManifest(manifest);
            SetStackFromManifest(manifest);
            SetBuildpacksFromManifest(manifest);
            SetServicesFromManifest(manifest);
            SetStartCommandFromManifest(manifest);
            SetPathFromManifest(manifest);
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
                ClearSelectedBuildpacks();

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

            if (svs != null)
            {
                ClearSelectedServices();

                foreach (var svName in svs)
                {
                    AddToSelectedServices(svName);

                    // mark corresponding service option as selected
                    var existingSvOption = ServiceOptions.FirstOrDefault(b => b.Name == svName);
                    if (existingSvOption != null)
                    {
                        existingSvOption.IsSelected = true;
                    }

                    ApplyWarningIfServiceNameNotPresentInOptions(svName);
                }
            }
        }

        private void ApplyWarningIfServiceNameNotPresentInOptions(string svName)
        {
            if (!ServiceOptions.Exists(s => s.Name == svName))
            {
                if (string.IsNullOrWhiteSpace(ServiceNotRecognizedWarningMessage))
                {
                    ServiceNotRecognizedWarningMessage = $"'{svName}' not recognized";
                }
                else
                {
                    ServiceNotRecognizedWarningMessage = "Multiple selected services not recognized";
                }
            }
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
            var startCmmd = appManifest.Applications[0].Command;

            StartCommand = string.IsNullOrWhiteSpace(startCmmd) ? null : startCmmd;
        }

        private void SetPathFromManifest(AppManifest appManifest)
        {
            var path = appManifest.Applications[0].Path;

            DeploymentDirectoryPath = string.IsNullOrWhiteSpace(path) ? null : path;
        }
    }

    public class BuildpackListItem : INotifyPropertyChanged
    {
        private string _name;
        private bool _isSelected;
        private bool _compatibleWithStack;

        public event PropertyChangedEventHandler PropertyChanged;

        public string Name
        {
            get => _name;
            set
            {
                _name = value;
            }
        }

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
            get { return _compatibleWithStack; }

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
        private string _name;
        private bool _isSelected;

        public event PropertyChangedEventHandler PropertyChanged;

        public string Name
        {
            get => _name;
            set
            {
                _name = value;
            }
        }

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
