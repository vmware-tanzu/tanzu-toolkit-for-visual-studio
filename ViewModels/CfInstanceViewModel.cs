using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using TanzuForVS.Models;

namespace TanzuForVS.ViewModels
{
    public class CfInstanceViewModel : TreeViewItemViewModel
    {
        public CloudFoundryInstance CloudFoundryInstance { get; }

        public CfInstanceViewModel(CloudFoundryInstance cloudFoundryInstance, IServiceProvider services)
            : base(null, services)
        {
            CloudFoundryInstance = cloudFoundryInstance;
            this.DisplayText = CloudFoundryInstance.InstanceName;
        }

        protected override async Task LoadChildren()
        {
            var orgs = await CloudFoundryService.GetOrgsForCfInstanceAsync(CloudFoundryInstance);
            if (orgs.Count == 0) DisplayText += " (no orgs)";

            var updatedOrgsList = new ObservableCollection<TreeViewItemViewModel>();
            foreach (CloudFoundryOrganization org in orgs)
            {
                var newOrg = new OrgViewModel(org, Services);
                updatedOrgsList.Add(newOrg);
            }

            Children = updatedOrgsList;
        }
    }
}
