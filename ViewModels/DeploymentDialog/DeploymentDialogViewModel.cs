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
        internal const string getOrgsFailureMsg = "Unable to fetch orgs.";
        private readonly string projDir;
        private string status;
        private string appName;
        private List<CloudFoundryInstance> cfInstances;
        private List<CloudFoundryOrganization> cfOrgs;
        private List<CloudFoundrySpace> cfSpaces;
        private CloudFoundryInstance selectedCf;
        private CloudFoundryOrganization selectedOrg;
        private CloudFoundrySpace selectedSpace;
        internal IOutputViewModel outputViewModel;


        public DeploymentDialogViewModel(IServiceProvider services, string directoryOfProjectToDeploy)
            : base(services)
        {
            IView outputView = ViewLocatorService.NavigateTo(nameof(OutputViewModel)) as IView;
            outputViewModel = outputView?.ViewModel as IOutputViewModel;

            DeploymentStatus = initialStatus;
            DeploymentInProgress = false;
            SelectedCf = null;
            projDir = directoryOfProjectToDeploy;


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
            try
            {
                await CloudFoundryService.DeployAppAsync(
                    SelectedSpace.ParentOrg.ParentCf,
                    SelectedSpace.ParentOrg,
                    SelectedSpace,
                    AppName,
                    projDir,
                    stdOutCallback: outputViewModel.AppendLine,
                    stdErrCallback: outputViewModel.AppendLine
                );
            }
            catch (Exception)
            {
            }

            DeploymentInProgress = false; 
        }

        public bool CanOpenLoginView(object arg)
        {
            return true;
        }

        public void OpenLoginView(object arg)
        {
            DialogService.ShowDialog(typeof(AddCloudDialogViewModel).Name);
            UpdateCfInstanceOptions();
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

                if (orgsResponse.Succeeded == false)
                {
                    DialogService.DisplayErrorDialog(getOrgsFailureMsg, orgsResponse.Explanation);
                }
                else
                {
                    CfOrgOptions = orgsResponse.Content;
                }
            }
        }

        public async Task UpdateCfSpaceOptions()
        {
            if (SelectedOrg == null) CfSpaceOptions = new List<CloudFoundrySpace>();

            else
            {
                var spaces = await CloudFoundryService.GetSpacesForOrgAsync(SelectedOrg);
                CfSpaceOptions = spaces;
            }
        }
    }
}