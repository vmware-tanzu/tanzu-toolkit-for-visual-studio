using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Tanzu.Toolkit.VisualStudio.Models;

namespace Tanzu.Toolkit.VisualStudio.ViewModels
{
    public class CfInstanceViewModel : TreeViewItemViewModel
    {
        public CloudFoundryInstance CloudFoundryInstance { get; }

        public CfInstanceViewModel(CloudFoundryInstance cloudFoundryInstance, IServiceProvider services)
            : base(null, services)
        {
            CloudFoundryInstance = cloudFoundryInstance;
            DisplayText = CloudFoundryInstance.InstanceName;
        }

        protected override async Task LoadChildren()
        {
            var orgs = await CloudFoundryService.GetOrgsForCfInstanceAsync(CloudFoundryInstance);

            if (orgs.Count == 0 && !DisplayText.Contains("(no orgs)")) DisplayText += " (no orgs)";

            var updatedOrgsList = new ObservableCollection<TreeViewItemViewModel>();
            foreach (CloudFoundryOrganization org in orgs)
            {
                var newOrg = new OrgViewModel(org, Services);
                updatedOrgsList.Add(newOrg);
            }

            Children = updatedOrgsList;
        }


        public async Task<List<OrgViewModel>> FetchChildren()
        {
            var newOrgsList = new List<OrgViewModel>();

            var orgs = await CloudFoundryService.GetOrgsForCfInstanceAsync(CloudFoundryInstance);
            foreach (CloudFoundryOrganization org in orgs)
            {
                var newOrg = new OrgViewModel(org, Services);
                newOrgsList.Add(newOrg);
            }

            return newOrgsList;
        }
    }
}
