using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Tanzu.Toolkit.VisualStudio.Models;

[assembly: InternalsVisibleTo("Tanzu.Toolkit.VisualStudio.ViewModel.Tests")]

namespace Tanzu.Toolkit.VisualStudio.ViewModels
{
    public class DeploymentDialogViewModel : AbstractViewModel, IDeploymentDialogViewModel
    {
        internal const string initialStatus = "Deployment hasn't started yet.";
        internal const string appNameEmptyMsg = "App name not specified.";
        internal const string targetEmptyMsg = "Target not specified.";
        internal const string orgEmptyMsg = "Org not specified.";
        internal const string spaceEmptyMsg = "Space not specified.";
        internal const string deploymentSuccessMsg = "App was successfully deployed!\nYou can now close this window.";
        internal const string deploymentErrorMsg = "Unable to deploy app:";
        internal const string getOrgsFailureMsg = "Unable to fetch orgs.";
        internal const string getSpacesFailureMsg = "Unable to fetch spaces.";

        private readonly string projDir;
        private readonly string projTargetFramework;
        private string status;
        private string appName;
        private readonly bool fullFrameworkDeployment = false;

        private List<CloudFoundryInstance> cfInstances;
        private List<CloudFoundryOrganization> cfOrgs;
        private List<CloudFoundrySpace> cfSpaces;
        private CloudFoundryInstance selectedCf;
        private CloudFoundryOrganization selectedOrg;
        private CloudFoundrySpace selectedSpace;

        internal IOutputViewModel outputViewModel;


        public DeploymentDialogViewModel(IServiceProvider services, string directoryOfProjectToDeploy, string targetFrameworkMoniker)
            : base(services)
        {
            IView outputView = ViewLocatorService.NavigateTo(nameof(OutputViewModel)) as IView;
            outputViewModel = outputView?.ViewModel as IOutputViewModel;

            DeploymentStatus = initialStatus;
            DeploymentInProgress = false;
            SelectedCf = null;
            projDir = directoryOfProjectToDeploy;

            if (targetFrameworkMoniker.StartsWith(".NETFramework")) fullFrameworkDeployment = true;

            UpdateCfInstanceOptions();
            CfOrgOptions = new List<CloudFoundryOrganization>();
            CfSpaceOptions = new List<CloudFoundrySpace>();
        }


        public string AppName
        {
            get => appName;

            set
            {
                appName = value;
                RaisePropertyChangedEvent("AppName");
            }
        }

        public string DeploymentStatus
        {

            get => status;

            set
            {
                status = value;
                RaisePropertyChangedEvent("DeploymentStatus");
            }
        }

        public CloudFoundryInstance SelectedCf
        {
            get => selectedCf;

            set
            {
                if (value != selectedCf)
                {
                    selectedCf = value;

                    // clear orgs & spaces
                    CfOrgOptions = new List<CloudFoundryOrganization>();
                    CfSpaceOptions = new List<CloudFoundrySpace>();

                    RaisePropertyChangedEvent("SelectedCf");
                }
            }
        }

        public CloudFoundryOrganization SelectedOrg
        {
            get => selectedOrg;

            set
            {
                if (value != selectedOrg)
                {
                    selectedOrg = value;

                    // clear spaces
                    CfSpaceOptions = new List<CloudFoundrySpace>();
                }

                RaisePropertyChangedEvent("SelectedOrg");
            }
        }

        public CloudFoundrySpace SelectedSpace
        {
            get => selectedSpace;

            set
            {
                if (value != selectedSpace)
                {
                    selectedSpace = value;
                }

                RaisePropertyChangedEvent("SelectedSpace");
            }
        }

        public List<CloudFoundryInstance> CfInstanceOptions
        {
            get => cfInstances;

            set
            {
                cfInstances = value;
                RaisePropertyChangedEvent("CfInstanceOptions");
            }
        }

        public List<CloudFoundryOrganization> CfOrgOptions
        {
            get => cfOrgs;

            set
            {
                cfOrgs = value;
                RaisePropertyChangedEvent("CfOrgOptions");
            }
        }

        public List<CloudFoundrySpace> CfSpaceOptions
        {
            get => cfSpaces;

            set
            {
                cfSpaces = value;
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
                DeploymentStatus = initialStatus;

                if (string.IsNullOrEmpty(AppName)) throw new Exception(appNameEmptyMsg);
                if (SelectedCf == null) throw new Exception(targetEmptyMsg);
                if (SelectedOrg == null) throw new Exception(orgEmptyMsg);
                if (SelectedSpace == null) throw new Exception(spaceEmptyMsg);

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

        internal async Task StartDeployment()
        {
            var deploymentResult = await CloudFoundryService.DeployAppAsync(
                SelectedSpace.ParentOrg.ParentCf,
                SelectedSpace.ParentOrg,
                SelectedSpace,
                AppName,
                projDir,
                fullFrameworkDeployment,
                stdOutCallback: outputViewModel.AppendLine,
                stdErrCallback: outputViewModel.AppendLine
            );

            if (!deploymentResult.Succeeded)
            {
                var errorTitle = $"{deploymentErrorMsg} {AppName}.";
                var errorMsg = deploymentResult.Explanation;

                Logger.Error(
                    "DeploymentDialogViewModel initiated app deployment of {AppName} to target {TargetApi}.{TargetOrg}.{TargetSpace}; deployment result reported failure: {DplmtResult}.",
                    AppName,
                    SelectedSpace.ParentOrg.ParentCf.ApiAddress,
                    SelectedSpace.ParentOrg.OrgName,
                    SelectedSpace.SpaceName,
                    deploymentResult.ToString()
                );
                DialogService.DisplayErrorDialog(errorTitle, errorMsg);
            }

            DeploymentInProgress = false;
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
                errorMsg += System.Environment.NewLine + "If you want to connect to a different cloud, please delete this one by right-clicking on it in the Cloud Explorer & re-connecting to a new one.";

                DialogService.DisplayErrorDialog(errorTitle, errorMsg);
            }
            else
            {
                DialogService.ShowDialog(typeof(AddCloudDialogViewModel).Name);
                UpdateCfInstanceOptions();
            }
        }

        public void UpdateCfInstanceOptions()
        {
            CfInstanceOptions = new List<CloudFoundryInstance>(CloudFoundryService.CloudFoundryInstances.Values);
        }

        public async Task UpdateCfOrgOptions()
        {
            if (SelectedCf == null) CfOrgOptions = new List<CloudFoundryOrganization>();

            else
            {
                var orgsResponse = await CloudFoundryService.GetOrgsForCfInstanceAsync(SelectedCf);

                if (orgsResponse.Succeeded)
                {
                    CfOrgOptions = orgsResponse.Content;
                }
                else
                {
                    Logger.Error($"{getOrgsFailureMsg}. {orgsResponse}");
                    DialogService.DisplayErrorDialog(getOrgsFailureMsg, orgsResponse.Explanation);
                }
            }
        }

        public async Task UpdateCfSpaceOptions()
        {
            if (SelectedOrg == null || SelectedCf == null) CfSpaceOptions = new List<CloudFoundrySpace>();

            else
            {
                var spacesResponse = await CloudFoundryService.GetSpacesForOrgAsync(SelectedOrg);

                if (spacesResponse.Succeeded)
                {
                    CfSpaceOptions = spacesResponse.Content;
                }
                else
                {
                    Logger.Error($"{getSpacesFailureMsg}. {spacesResponse}");
                    DialogService.DisplayErrorDialog(getSpacesFailureMsg, spacesResponse.Explanation);
                }
            }
        }
    }
}