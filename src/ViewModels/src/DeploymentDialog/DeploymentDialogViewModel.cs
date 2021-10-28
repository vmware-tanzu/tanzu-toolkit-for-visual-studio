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
        private string _projectName;
        private string _manifestPathLabel;
        private string _manifestPath;
        private string _directoryPathLabel;
        private string _directoryPath;
        private string _targetName;
        private bool _isLoggedIn;
        private string _selectedStack;
        private ObservableCollection<string> _selectedBuildpacks;
        private List<string> _stackOptions = new List<string> { "windows", "cflinuxfs3" };
        private List<BuildpackListItem> _buildpackOptions;
        private bool _expanded;
        private string _expansionButtonText;
        private AppManifest _appManifest;

        public DeploymentDialogViewModel(IServiceProvider services, string projectName, string directoryOfProjectToDeploy, string targetFrameworkMoniker)
            : base(services)
        {
            _errorDialogService = services.GetRequiredService<IErrorDialog>();
            TasExplorerViewModel = services.GetRequiredService<ITasExplorerViewModel>();

            IView outputView = ViewLocatorService.NavigateTo(nameof(ViewModels.OutputViewModel)) as IView;
            OutputViewModel = outputView?.ViewModel as IOutputViewModel;

            DeploymentInProgress = false;
            PathToProjectRootDir = directoryOfProjectToDeploy;
            SelectedBuildpacks = new ObservableCollection<string>();

            if (targetFrameworkMoniker.StartsWith(FullFrameworkTFM))
            {
                _fullFrameworkDeployment = true;
                _stackOptions = new List<string> { "windows" };
            }

            CfInstanceOptions = new List<CloudFoundryInstance>();
            CfOrgOptions = new List<CloudFoundryOrganization>();
            CfSpaceOptions = new List<CloudFoundrySpace>();
            BuildpackOptions = new List<BuildpackListItem>();
            ManifestModel = new AppManifest
            {
                Version = 1,
                Applications = new List<AppConfig>
                {
                    new AppConfig
                    {
                        Name = projectName,
                        Path = directoryOfProjectToDeploy,
                    }
                }
            };

            SetManifestIfDefaultExists();

            if (TasExplorerViewModel.TasConnection != null)
            {
                TargetName = TasExplorerViewModel.TasConnection.DisplayText;
                IsLoggedIn = true;

                ThreadingService.StartTask(UpdateCfOrgOptions);
                ThreadingService.StartTask(UpdateBuildpackOptions);
            }

            DeploymentDirectoryPath = PathToProjectRootDir;
            _projectName = projectName;

            Expanded = false;
        }

        public string AppName
        {
            get => _appName;

            set
            {
                _appName = value;
                RaisePropertyChangedEvent("AppName");
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

                    var parsingResult = CloudFoundryService.ParseManifestFile(_manifestPath);

                    if (parsingResult.Succeeded)
                    {
                        ManifestModel = parsingResult.Content;
                        SetViewModelValuesFromManifest(ManifestModel);
                    }
                    else
                    {
                        _errorDialogService.DisplayErrorDialog(ManifestParsingErrorTitle, parsingResult.Explanation);
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
                    _errorDialogService.DisplayErrorDialog(DirectoryNotFoundTitle, $"'{value}' does not appear to be a valid path to a directory.");
                    _directoryPath = null;
                    DirectoryPathLabel = "<none specified>";
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
                if (_stackOptions.Contains(value))
                {
                    _selectedStack = value;
                    RaisePropertyChangedEvent("SelectedStack");

                    ManifestModel.Applications[0].Stack = value;
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

        public bool CanDeployApp(object arg)
        {
            return !string.IsNullOrEmpty(AppName) && IsLoggedIn && SelectedOrg != null && SelectedSpace != null;
        }

        public void DeployApp(object dialogWindow)
        {
            if (CanDeployApp(null))
            {
                DeploymentInProgress = true;

                ThreadingService.StartTask(StartDeployment);

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

                ThreadingService.StartTask(UpdateCfOrgOptions);
                ThreadingService.StartTask(UpdateBuildpackOptions);
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
                var buildpacksRespsonse = await CloudFoundryService.GetUniqueBuildpackNamesAsync(TasExplorerViewModel.TasConnection.CloudFoundryInstance.ApiAddress);

                if (buildpacksRespsonse.Succeeded)
                {
                    var bpOtps = new List<BuildpackListItem>();

                    foreach (string bpName in buildpacksRespsonse.Content)
                    {
                        bool nameSpecifiedInManifest = ManifestModel.Applications[0].Buildpacks.Contains(bpName);

                        bpOtps.Add(new BuildpackListItem { Name = bpName, IsSelected = nameSpecifiedInManifest });
                    }

                    BuildpackOptions = bpOtps;
                }
                else
                {
                    BuildpackOptions = new List<BuildpackListItem>();

                    Logger.Error(GetBuildpacksFailureMsg + " {BuildpacksResponseError}", buildpacksRespsonse.Explanation);
                    _errorDialogService.DisplayErrorDialog(GetBuildpacksFailureMsg, buildpacksRespsonse.Explanation);
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

        internal async Task StartDeployment()
        {
            var deploymentResult = await CloudFoundryService.DeployAppAsync(
                ManifestModel,
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
            var bps = appManifest.Applications[0].Buildpacks;
            if (bps != null)
            {
                SelectedBuildpacks.Clear();
                foreach (string bp in appManifest.Applications[0].Buildpacks)
                {
                    AddToSelectedBuildpacks(bp);
                }
            }
        }
    }

    public class BuildpackListItem : INotifyPropertyChanged
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
            var handler = PropertyChanged;

            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
}