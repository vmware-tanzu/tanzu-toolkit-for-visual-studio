using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using TanzuForVS.Models;
using TanzuForVS.Services;

[assembly: InternalsVisibleTo("TanzuForVS.ViewModel.Tests")]

namespace TanzuForVS.ViewModels
{
    public class DeploymentDialogViewModel : AbstractViewModel, IDeploymentDialogViewModel
    {
        internal const string initialStatus = "Deployment hasn't started yet.";
        internal const string appNameEmptyMsg = "App name not specified.";
        internal const string targetEmptyMsg = "Target not specified.";
        internal const string orgEmptyMsg = "Org not specified.";
        internal const string spaceEmptyMsg = "Space not specified.";
        internal const string deploymentSuccessMsg = "App was successfully deployed!\nYou can now close this window.";
        private readonly string projDir;
        private string status;
        private string appName;
        private List<CloudFoundryInstance> cfInstances;
        private List<CloudFoundryOrganization> cfOrgs;
        private List<CloudFoundrySpace> cfSpaces;
        private CloudFoundryInstance selectedCf;
        private CloudFoundryOrganization selectedOrg;
        private CloudFoundrySpace selectedSpace;


        public DeploymentDialogViewModel(IServiceProvider services, string directoryOfProjectToDeploy)
            : base(services)
        {
            DeploymentStatus = initialStatus;
            SelectedCf = null;
            projDir = directoryOfProjectToDeploy;
            UpdateCfInstanceOptions();
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

            get => this.status;

            set
            {
                this.status = value;
                this.RaisePropertyChangedEvent("DeploymentStatus");
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
                this.RaisePropertyChangedEvent("CfInstanceOptions");
            }
        }

        public List<CloudFoundryOrganization> CfOrgOptions
        {
            get => cfOrgs;

            set
            {
                cfOrgs = value;
                this.RaisePropertyChangedEvent("CfOrgOptions");
            }
        }

        public List<CloudFoundrySpace> CfSpaceOptions
        {
            get => cfSpaces;

            set
            {
                cfSpaces = value;
                this.RaisePropertyChangedEvent("CfSpaceOptions");
            }
        }


        public bool CanDeployApp(object arg)
        {
            return true;
        }

        public async Task DeployApp(object arg)
        {
            try
            {
                DeploymentStatus = initialStatus;

                if (string.IsNullOrEmpty(AppName)) throw new Exception(appNameEmptyMsg);
                if (SelectedCf == null) throw new Exception(targetEmptyMsg);
                if (SelectedOrg == null) throw new Exception(orgEmptyMsg);
                if (SelectedSpace == null) throw new Exception(spaceEmptyMsg);

                DeploymentStatus = "Waiting for app to deploy....";

                DetailedResult appDeployment = await CloudFoundryService.DeployAppAsync(SelectedSpace.ParentOrg.ParentCf,
                                                                               SelectedSpace.ParentOrg,
                                                                               SelectedSpace,
                                                                               AppName,
                                                                               projDir,
                                                                               UpdateDeploymentStatus);

                if (appDeployment.Succeeded) DeploymentStatus += $"\n{deploymentSuccessMsg}";
                else DeploymentStatus += '\n' + appDeployment.Explanation;
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
                var orgs = await CloudFoundryService.GetOrgsForCfInstanceAsync(SelectedCf);
                CfOrgOptions = orgs;
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

        private void UpdateDeploymentStatus(string content)
        {
            DeploymentStatus += $"\n{content}";
        }

    }
}