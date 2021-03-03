using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Tanzu.Toolkit.VisualStudio.Models;

namespace Tanzu.Toolkit.VisualStudio.ViewModels
{
    public class CfInstanceViewModel : TreeViewItemViewModel
    {
        internal const string emptyOrgsPlaceholderMsg = "No orgs";
        internal const string loadingMsg = "Loading orgs...";

        public CloudFoundryInstance CloudFoundryInstance { get; }

        public CfInstanceViewModel(CloudFoundryInstance cloudFoundryInstance, IServiceProvider services)
            : base(null, services)
        {
            CloudFoundryInstance = cloudFoundryInstance;
            DisplayText = CloudFoundryInstance.InstanceName;

            LoadingPlaceholder = new PlaceholderViewModel(parent: this, services)
            {
                DisplayText = loadingMsg
            };
        }

        internal protected override async Task LoadChildren()
        {
            var orgs = await CloudFoundryService.GetOrgsForCfInstanceAsync(CloudFoundryInstance);

            if (orgs.Count == 0)
            {
                var noChildrenList = new ObservableCollection<TreeViewItemViewModel>
                {
                    new PlaceholderViewModel(parent: this, Services)
                    {
                        DisplayText = emptyOrgsPlaceholderMsg
                    }
                };

                Children = noChildrenList;
            }
            else
            {
                var updatedOrgsList = new ObservableCollection<TreeViewItemViewModel>();
                foreach (CloudFoundryOrganization org in orgs)
                {
                    var newOrg = new OrgViewModel(org, Services);
                    updatedOrgsList.Add(newOrg);
                }

                Children = updatedOrgsList;
            }

            IsLoading = false;
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
