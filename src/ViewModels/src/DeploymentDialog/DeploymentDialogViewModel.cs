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
        internal const string DeploymentErrorMsg = "Unable to deploy app:";
        internal const string GetOrgsFailureMsg = "Unable to fetch orgs.";
        internal const string GetSpacesFailureMsg = "Unable to fetch spaces.";

        private string _status;
        private string _appName;
        private readonly bool _fullFrameworkDeployment = false;
        private readonly IErrorDialog _dialogService;
        internal IOutputViewModel OutputViewModel;
        internal ITasExplorerViewModel TasExplorerViewModel;

        private List<CloudFoundryInstance> _cfInstances;
        private List<CloudFoundryOrganization> _cfOrgs;
        private List<CloudFoundrySpace> _cfSpaces;
        private CloudFoundryInstance _selectedCf;
        private CloudFoundryOrganization _selectedOrg;
        private CloudFoundrySpace _selectedSpace;
        private string _manifestPathLabel;
        private string _manifestPath;

        public DeploymentDialogViewModel(IServiceProvider services, string directoryOfProjectToDeploy, string targetFrameworkMoniker)
            : base(services)
        {
            _dialogService = services.GetRequiredService<IErrorDialog>();

            IView outputView = ViewLocatorService.NavigateTo(nameof(ViewModels.OutputViewModel)) as IView;
            OutputViewModel = outputView?.ViewModel as IOutputViewModel;

            IView tasExplorerView = ViewLocatorService.NavigateTo(nameof(ViewModels.TasExplorerViewModel)) as IView;
            TasExplorerViewModel = tasExplorerView?.ViewModel as ITasExplorerViewModel;

            DeploymentStatus = InitialStatus;
            DeploymentInProgress = false;
            SelectedCf = null;
            ProjectDirPath = directoryOfProjectToDeploy;

            if (targetFrameworkMoniker.StartsWith(".NETFramework"))
            {
                _fullFrameworkDeployment = true;
            }

            UpdateCfInstanceOptions();
            CfOrgOptions = new List<CloudFoundryOrganization>();
            CfSpaceOptions = new List<CloudFoundrySpace>();

            SetManifestIfDefaultExists();
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

        public string ProjectDirPath { get; private set; }

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

                    ManifestPathLabel = value;
                    SetAppNameFromManifest(value);
                }
                else
                {
                    _dialogService.DisplayErrorDialog("Unable to set manifest path", $"'{value}' does not appear to be a valid path to a manifest.");
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

        public CloudFoundryInstance SelectedCf
        {
            get => _selectedCf;

            set
            {
                if (value != _selectedCf)
                {
                    _selectedCf = value;

                    // clear orgs & spaces
                    CfOrgOptions = new List<CloudFoundryOrganization>();
                    CfSpaceOptions = new List<CloudFoundrySpace>();

                    RaisePropertyChangedEvent("SelectedCf");
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

        public bool DeploymentInProgress { get; internal set; }

        public bool CanDeployApp(object arg)
        {
            return true;
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

                if (SelectedCf == null)
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
            if (CloudFoundryService.CloudFoundryInstances.Count > 0)
            {
                var errorTitle = "Unable to add more TAS connections.";
                var errorMsg = "This version of Tanzu Toolkit for Visual Studio only supports 1 cloud connection at a time; multi-cloud connections will be supported in the future.";
                errorMsg += System.Environment.NewLine + "If you want to connect to a different cloud, please delete this one by right-clicking on it in the Tanzu Application Service Explorer & re-connecting to a new one.";

                _dialogService.DisplayErrorDialog(errorTitle, errorMsg);
            }
            else
            {
                DialogService.ShowDialog(typeof(LoginViewModel).Name);
                UpdateCfInstanceOptions();
            }
        }

        public void UpdateCfInstanceOptions()
        {
            CfInstanceOptions = new List<CloudFoundryInstance>(CloudFoundryService.CloudFoundryInstances.Values);
        }

        public async Task UpdateCfOrgOptions()
        {
            if (SelectedCf == null)
            {
                CfOrgOptions = new List<CloudFoundryOrganization>();
            }
            else
            {
                var orgsResponse = await CloudFoundryService.GetOrgsForCfInstanceAsync(SelectedCf);

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
            if (SelectedOrg == null || SelectedCf == null)
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
                ProjectDirPath,
                _fullFrameworkDeployment,
                stdOutCallback: OutputViewModel.AppendLine,
                stdErrCallback: OutputViewModel.AppendLine,
                manifestPath: ManifestPath);

            if (!deploymentResult.Succeeded)
            {
                if (deploymentResult.FailureType == Toolkit.Services.FailureType.InvalidRefreshToken)
                {
                    TasExplorerViewModel.AuthenticationRequired = true;
                }

                var errorTitle = $"{DeploymentErrorMsg} {AppName}.";
                var errorMsg = deploymentResult.Explanation;

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
            var expectedManifestLocation1 = Path.Combine(ProjectDirPath, "manifest.yaml");
            var expectedManifestLocation2 = Path.Combine(ProjectDirPath, "manifest.yml");

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

        private void SetAppNameFromManifest(string pathToManifest)
        {
            string[] manifestContents = File.ReadAllLines(pathToManifest);

            foreach (string line in manifestContents)
            {
                if (line.StartsWith("- name"))
                {
                    AppName = line.Substring(line.IndexOf(":") + 1).Trim();
                }
            }
        }
    }
}