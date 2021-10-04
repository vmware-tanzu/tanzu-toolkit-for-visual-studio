using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Tanzu.Toolkit.Models;
using Tanzu.Toolkit.Services.ErrorDialog;

[assembly: InternalsVisibleTo("Tanzu.Toolkit.ViewModels.Tests")]

namespace Tanzu.Toolkit.ViewModels
{
    public class DeploymentDialogViewModel : AbstractViewModel, IDeploymentDialogViewModel
    {
        internal const string InitialStatus = "Deployment hasn't started yet.";
        internal const string AppNameEmptyMsg = "App name not specified.";
        internal const string TargetEmptyMsg = "Target not specified.";
        internal const string OrgEmptyMsg = "Org not specified.";
        internal const string SpaceEmptyMsg = "Space not specified.";
        internal const string DeploymentSuccessMsg = "App was successfully deployed!\nYou can now close this window.";
        internal const string DeploymentErrorMsg = "Encountered an issue while deploying app:";
        internal const string GetOrgsFailureMsg = "Unable to fetch orgs.";
        internal const string GetSpacesFailureMsg = "Unable to fetch spaces.";
        internal const string SingleLoginErrorTitle = "Unable to add more TAS connections.";
        internal const string SingleLoginErrorMessage1 = "This version of Tanzu Toolkit for Visual Studio only supports 1 cloud connection at a time; multi-cloud connections will be supported in the future.";
        internal const string SingleLoginErrorMessage2 = "If you want to connect to a different cloud, please delete this one by right-clicking on it in the Tanzu Application Service Explorer & re-connecting to a new one.";
        internal const string FullFrameworkTFM = ".NETFramework";
        internal const string ManifestNotFoundTitle = "Unable to set manifest path";
        internal const string DirectoryNotFoundTitle = "Unable to set push directory path";

        private string _status;
        private string _appName;
        internal readonly bool _fullFrameworkDeployment = false;
        private readonly IErrorDialog _dialogService;
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
        private List<string> _stackOptions = new List<string> { "windows", "cflinuxfs3" };
        private bool _binaryDeployment;
        private string _deploymentButtonLabel;

        public DeploymentDialogViewModel(IServiceProvider services, string projectName, string directoryOfProjectToDeploy, string targetFrameworkMoniker)
            : base(services)
        {
            _dialogService = services.GetRequiredService<IErrorDialog>();
            TasExplorerViewModel = services.GetRequiredService<ITasExplorerViewModel>();

            IView outputView = ViewLocatorService.NavigateTo(nameof(ViewModels.OutputViewModel)) as IView;
            OutputViewModel = outputView?.ViewModel as IOutputViewModel;

            DeploymentStatus = InitialStatus;
            DeploymentInProgress = false;
            PathToProjectRootDir = directoryOfProjectToDeploy;

            if (targetFrameworkMoniker.StartsWith(FullFrameworkTFM))
            {
                _fullFrameworkDeployment = true;
                _stackOptions = new List<string> { "windows" };
            }

            CfInstanceOptions = new List<CloudFoundryInstance>();
            CfOrgOptions = new List<CloudFoundryOrganization>();
            CfSpaceOptions = new List<CloudFoundrySpace>();

            SetManifestIfDefaultExists();

            if (TasExplorerViewModel.TasConnection != null)
            {
                TargetName = TasExplorerViewModel.TasConnection.DisplayText;
                IsLoggedIn = true;

                ThreadingService.StartTask(UpdateCfOrgOptions);
            }

            DeploymentDirectoryPath = PathToProjectRootDir;
            _projectName = projectName;
        }

        public bool BinaryDeployment
        {
            get => _binaryDeployment;

            internal set
            {
                DeploymentButtonLabel = value ? "Push app (from binaries)" : "Push app (from source)";
                _binaryDeployment = value;
                RaisePropertyChangedEvent("BinaryDeployment");
            }
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

        public string DeploymentStatus
        {
            get => _status;

            set
            {
                _status = value;
                RaisePropertyChangedEvent("DeploymentStatus");
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
                else if (File.Exists(value))
                {
                    _manifestPath = value;

                    ManifestPathLabel = _manifestPath;

                    string[] manifestLines = File.ReadAllLines(_manifestPath);
                    SetAppNameFromManifest(manifestLines);
                    SetStackFromManifest(manifestLines);
                }
                else
                {
                    _dialogService.DisplayErrorDialog(ManifestNotFoundTitle, $"'{value}' does not appear to be a valid path to a manifest.");
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
                if (Directory.Exists(value))
                {
                    _directoryPath = value;
                    DirectoryPathLabel = value;

                    BinaryDeployment = value != PathToProjectRootDir;
                }
                else
                {
                    _dialogService.DisplayErrorDialog(DirectoryNotFoundTitle, $"'{value}' does not appear to be a valid path to a directory.");
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

        public string DeploymentButtonLabel
        {
            get => _deploymentButtonLabel;

            set
            {
                _deploymentButtonLabel = value;
                RaisePropertyChangedEvent("DeploymentButtonLabel");
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
                }
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

        public bool CanDeployApp(object arg)
        {
            return !string.IsNullOrEmpty(AppName) && IsLoggedIn && SelectedOrg != null && SelectedSpace != null;
        }

        public void DeployApp(object dialogWindow)
        {
            try
            {
                DeploymentStatus = InitialStatus;

                if (string.IsNullOrEmpty(AppName))
                {
                    throw new Exception(AppNameEmptyMsg);
                }

                if (TasExplorerViewModel.TasConnection == null)
                {
                    throw new Exception(TargetEmptyMsg);
                }

                if (SelectedOrg == null)
                {
                    throw new Exception(OrgEmptyMsg);
                }

                if (SelectedSpace == null)
                {
                    throw new Exception(SpaceEmptyMsg);
                }

                DeploymentStatus = "Waiting for app to deploy....";

                DeploymentInProgress = true;
                Task.Run(StartDeployment);

                DialogService.CloseDialog(dialogWindow, true);
            }
            catch (Exception e)
            {
                DeploymentStatus += $"\nAn error occurred: \n{e.Message}";
            }
        }

        public bool CanOpenLoginView(object arg)
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
                    _dialogService.DisplayErrorDialog(GetOrgsFailureMsg, orgsResponse.Explanation);
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
                    _dialogService.DisplayErrorDialog(GetSpacesFailureMsg, spacesResponse.Explanation);
                }
            }
        }

        internal async Task StartDeployment()
        {
            var deploymentResult = await CloudFoundryService.DeployAppAsync(
                SelectedSpace.ParentOrg.ParentCf,
                SelectedSpace.ParentOrg,
                SelectedSpace,
                AppName,
                DeploymentDirectoryPath,
                _fullFrameworkDeployment,
                stdOutCallback: OutputViewModel.AppendLine,
                stdErrCallback: OutputViewModel.AppendLine,
                stack: SelectedStack,
                binaryDeployment: BinaryDeployment,
                projectName: _projectName, 
                manifestPath: ManifestPath);

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

                _dialogService.DisplayErrorDialog(errorTitle, errorMsg);
            }

            DeploymentInProgress = false;
        }

        private void SetManifestIfDefaultExists()
        {
            var expectedManifestLocation1 = Path.Combine(PathToProjectRootDir, "manifest.yaml");
            var expectedManifestLocation2 = Path.Combine(PathToProjectRootDir, "manifest.yml");

            if (File.Exists(expectedManifestLocation1))
            {
                ManifestPath = expectedManifestLocation1;
            }
            else if (File.Exists(expectedManifestLocation2))
            {
                ManifestPath = expectedManifestLocation2;
            }
            else
            {
                ManifestPath = null;
            }
        }

        private void SetAppNameFromManifest(string[] manifestContents)
        {
            foreach (string line in manifestContents)
            {
                if (line.StartsWith("- name"))
                {
                    AppName = line.Substring(line.IndexOf(":") + 1).Trim();
                }
            }
        }

        private void SetStackFromManifest(string[] manifestContents)
        {
            foreach (string line in manifestContents)
            {
                if (line.Contains("stack: "))
                {
                    var detectedStack = line.Substring(line.IndexOf(":") + 1).Trim();

                    if (_stackOptions.Contains(detectedStack))
                    {
                        SelectedStack = detectedStack;
                    }
                }
            }
        }
    }
}