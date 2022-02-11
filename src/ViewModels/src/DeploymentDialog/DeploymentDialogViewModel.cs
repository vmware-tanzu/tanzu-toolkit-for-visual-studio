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
using Tanzu.Toolkit.Services.ErrorDialog;

[assembly: InternalsVisibleTo("Tanzu.Toolkit.ViewModels.Tests")]

namespace Tanzu.Toolkit.ViewModels
{
    public class DeploymentDialogViewModel : AbstractViewModel, IDeploymentDialogViewModel
    {
        internal const string AppNameEmptyMsg = "App name not specified.";
        internal const string TargetEmptyMsg = "Target not specified.";
        internal const string OrgEmptyMsg = "Org not specified.";
        internal const string SpaceEmptyMsg = "Space not specified.";
        internal const string DeploymentSuccessMsg = "App was successfully deployed!\nYou can now close this window.";
        internal const string DeploymentErrorMsg = "Encountered an issue while deploying app:";
        internal const string GetOrgsFailureMsg = "Unable to fetch orgs.";
        internal const string GetSpacesFailureMsg = "Unable to fetch spaces.";
        internal const string GetBuildpacksFailureMsg = "Unable to fetch buildpacks.";
        internal const string GetStacksFailureMsg = "Unable to fetch stacks.";
        internal const string SingleLoginErrorTitle = "Unable to add more TAS connections.";
        internal const string SingleLoginErrorMessage1 = "This version of Tanzu Toolkit for Visual Studio only supports 1 cloud connection at a time; multi-cloud connections will be supported in the future.";
        internal const string SingleLoginErrorMessage2 = "If you want to connect to a different cloud, please delete this one by right-clicking on it in the Tanzu Application Service Explorer & re-connecting to a new one.";
        internal const string FullFrameworkTFM = ".NETFramework";
        internal const string ManifestNotFoundTitle = "Unable to set manifest path";
        internal const string ManifestParsingErrorTitle = "Unable to parse app manifest";
        internal const string DirectoryNotFoundTitle = "Unable to set push directory path";

        private string _appName;
        internal readonly bool _fullFrameworkDeployment = false;
        private readonly IErrorDialog _errorDialogService;
        internal IOutputViewModel OutputViewModel;
        internal ITasExplorerViewModel TasExplorerViewModel;

        private List<CloudFoundryInstance> _cfInstances;
        private List<CloudFoundryOrganization> _cfOrgs;
        private List<CloudFoundrySpace> _cfSpaces;
        private CloudFoundryOrganization _selectedOrg;
        private CloudFoundrySpace _selectedSpace;
        private string _startCmmd;
        private string _projectName;
        private string _manifestPathLabel;
        private string _manifestPath;
        private string _directoryPathLabel;
        private string _directoryPath;
        private string _targetName;
        private bool _isLoggedIn;
        private string _selectedStack;
        private ObservableCollection<string> _selectedBuildpacks;
        private List<string> _stackOptions;
        private List<BuildpackListItem> _buildpackOptions;
        private bool _expanded;
        private string _expansionButtonText;
        private AppManifest _appManifest;
        private bool _buildpacksLoading = false;
        private bool _stacksLoading = false;

        public DeploymentDialogViewModel(IServiceProvider services, string projectName, string directoryOfProjectToDeploy, string targetFrameworkMoniker)
            : base(services)
        {
            _errorDialogService = services.GetRequiredService<IErrorDialog>();
            TasExplorerViewModel = services.GetRequiredService<ITasExplorerViewModel>();

            OutputView = ViewLocatorService.GetViewByViewModelName(nameof(ViewModels.OutputViewModel)) as IView;
            OutputViewModel = OutputView?.ViewModel as IOutputViewModel;

            DeploymentInProgress = false;
            PathToProjectRootDir = directoryOfProjectToDeploy;
            SelectedBuildpacks = new ObservableCollection<string>();

            if (targetFrameworkMoniker.StartsWith(FullFrameworkTFM))
            {
                _fullFrameworkDeployment = true;
            }

            CfInstanceOptions = new List<CloudFoundryInstance>();
            CfOrgOptions = new List<CloudFoundryOrganization>();
            CfSpaceOptions = new List<CloudFoundrySpace>();
            BuildpackOptions = new List<BuildpackListItem>();
            StackOptions = new List<string>();
            DeploymentDirectoryPath = null;

            ManifestModel = new AppManifest
            {
                Version = 1,
                Applications = new List<AppConfig>
                {
                    new AppConfig
                    {
                        Name = projectName,
                        Buildpacks = new List<string>(),
                    }
                }
            };

            SetManifestIfDefaultExists();

            if (TasExplorerViewModel.TasConnection != null)
            {
                TargetName = TasExplorerViewModel.TasConnection.DisplayText;
                IsLoggedIn = true;

                ThreadingService.StartBackgroundTask(UpdateCfOrgOptions);
                ThreadingService.StartBackgroundTask(UpdateBuildpackOptions);
                ThreadingService.StartBackgroundTask(UpdateStackOptions);
            }

            _projectName = projectName;

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
                        string manifestContents = FileService.ReadFileContents(value);

                        AppManifest parsedManifest = SerializationService.ParseCfAppManifest(manifestContents);

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
                        AppManifest modelInstance = parsedManifest.DeepClone();

                        ManifestModel = modelInstance;
                        SetViewModelValuesFromManifest(parsedManifest);
                    }
                    catch (Exception ex)
                    {
                        _errorDialogService.DisplayErrorDialog(ManifestParsingErrorTitle, ex.Message);
                    }
                }
                else
                {
                    _errorDialogService.DisplayErrorDialog(ManifestNotFoundTitle, $"'{value}' does not appear to be a valid path to a manifest.");
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
                if (FileService.DirectoryExists(value))
                {
                    _directoryPath = value;
                    DirectoryPathLabel = value;

                    ManifestModel.Applications[0].Path = value;
                }
                else
                {
                    if (value != null)
                    {
                        _errorDialogService.DisplayErrorDialog(DirectoryNotFoundTitle, $"'{value}' does not appear to be a valid path to a directory.");

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

                foreach (BuildpackListItem b in BuildpackOptions)
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

        public bool StacksLoading
        {
            get { return _stacksLoading; }

            set
            {
                _stacksLoading = value;
                RaisePropertyChangedEvent("StacksLoading");
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

                ThreadingService.StartBackgroundTask(StartDeployment);

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
            TasExplorerViewModel.OpenLoginView(arg);

            if (TasExplorerViewModel.TasConnection != null)
            {
                CfInstanceOptions = new List<CloudFoundryInstance>
                {
                    TasExplorerViewModel.TasConnection.CloudFoundryInstance,
                };

                TargetName = TasExplorerViewModel.TasConnection.DisplayText;
                IsLoggedIn = true;

                ThreadingService.StartBackgroundTask(UpdateCfOrgOptions);
                ThreadingService.StartBackgroundTask(UpdateBuildpackOptions);
                ThreadingService.StartBackgroundTask(UpdateStackOptions);
            }
        }

        public async Task UpdateCfOrgOptions()
        {
            if (TasExplorerViewModel.TasConnection == null)
            {

                CfOrgOptions = new List<CloudFoundryOrganization>();
            }
            else
            {
                var orgsResponse = await CloudFoundryService.GetOrgsForCfInstanceAsync(TasExplorerViewModel.TasConnection.CloudFoundryInstance);

                if (orgsResponse.Succeeded)
                {
                    CfOrgOptions = orgsResponse.Content;
                }
                else
                {
                    Logger.Error($"{GetOrgsFailureMsg}. {orgsResponse}");
                    _errorDialogService.DisplayErrorDialog(GetOrgsFailureMsg, orgsResponse.Explanation);
                }
            }
        }

        public async Task UpdateCfSpaceOptions()
        {
            if (SelectedOrg == null || TasExplorerViewModel.TasConnection == null)
            {
                CfSpaceOptions = new List<CloudFoundrySpace>();
            }
            else
            {
                var spacesResponse = await CloudFoundryService.GetSpacesForOrgAsync(SelectedOrg);

                if (spacesResponse.Succeeded)
                {
                    CfSpaceOptions = spacesResponse.Content;
                }
                else
                {
                    Logger.Error($"{GetSpacesFailureMsg}. {spacesResponse}");
                    _errorDialogService.DisplayErrorDialog(GetSpacesFailureMsg, spacesResponse.Explanation);
                }
            }
        }

        public async Task UpdateBuildpackOptions()
        {
            if (TasExplorerViewModel.TasConnection == null)
            {
                BuildpackOptions = new List<BuildpackListItem>();
            }
            else
            {
                BuildpacksLoading = true;
                var buildpacksRespsonse = await CloudFoundryService.GetBuildpacksAsync(TasExplorerViewModel.TasConnection.CloudFoundryInstance.ApiAddress);

                if (buildpacksRespsonse.Succeeded)
                {
                    var bpOtps = new List<BuildpackListItem>();

                    foreach (CfBuildpack bp in buildpacksRespsonse.Content)
                    {
                        bool nameSpecifiedInManifest = ManifestModel.Applications[0].Buildpacks.Contains(bp.Name);
                        bool bpCompatibleWithSelectedStack = SelectedStack == null || SelectedStack == bp.Stack;
                        bool nameAlreadyExistsInOptions = bpOtps.Any(b => b.Name == bp.Name);

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

                    Logger.Error(GetBuildpacksFailureMsg + " {BuildpacksResponseError}", buildpacksRespsonse.Explanation);
                    _errorDialogService.DisplayErrorDialog(GetBuildpacksFailureMsg, buildpacksRespsonse.Explanation);
                }
            }
        }

        public async Task UpdateStackOptions()
        {
            if (TasExplorerViewModel.TasConnection == null)
            {
                StackOptions = new List<string>();
            }
            else
            {
                StacksLoading = true;
                var stacksRespsonse = await CloudFoundryService.GetStackNamesAsync(TasExplorerViewModel.TasConnection.CloudFoundryInstance);

                if (stacksRespsonse.Succeeded)
                {
                    StackOptions = stacksRespsonse.Content;
                    StacksLoading = false;
                }
                else
                {
                    StackOptions = new List<string>();
                    StacksLoading = false;

                    Logger.Error(GetStacksFailureMsg + " {StacksResponseError}", stacksRespsonse.Explanation);
                    _errorDialogService.DisplayErrorDialog(GetStacksFailureMsg, stacksRespsonse.Explanation);
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

            foreach (BuildpackListItem bpItem in BuildpackOptions)
            {
                bpItem.IsSelected = false;
            }

            RaisePropertyChangedEvent("SelectedBuildpacks");
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

                Logger.Error(errorMsg);

                _errorDialogService.DisplayErrorDialog("Unable to save manifest file", errorMsg);
            }
        }

        internal async Task StartDeployment()
        {
            var deploymentResult = await CloudFoundryService.DeployAppAsync(
                ManifestModel,
                PathToProjectRootDir,
                SelectedSpace.ParentOrg.ParentCf,
                SelectedSpace.ParentOrg,
                SelectedSpace,
                stdOutCallback: OutputViewModel.AppendLine,
                stdErrCallback: OutputViewModel.AppendLine);

            if (!deploymentResult.Succeeded)
            {
                if (deploymentResult.FailureType == Toolkit.Services.FailureType.InvalidRefreshToken)
                {
                    TasExplorerViewModel.AuthenticationRequired = true;
                }

                var errorTitle = $"{DeploymentErrorMsg} {AppName}.";
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
            AppConfig appConfig = appManifest.Applications[0];

            var bps = appConfig.Buildpacks;
            var stack = appConfig.Stack;

            if (bps != null)
            {
                ClearSelectedBuildpacks();

                foreach (string bpName in bps)
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
            var handler = PropertyChanged;

            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
}
