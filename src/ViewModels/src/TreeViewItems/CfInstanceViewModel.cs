using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Tanzu.Toolkit.Models;

namespace Tanzu.Toolkit.ViewModels
{
    public class CfInstanceViewModel : TreeViewItemViewModel
    {
        /* ERROR MESSAGE CONSTANTS */
        internal static readonly string _emptyOrgsPlaceholderMsg = "No orgs";
        internal static readonly string _loadingMsg = "Loading orgs...";
        internal static readonly string _getOrgsFailureMsg = "Unable to load orgs";

        public CloudFoundryInstance CloudFoundryInstance { get; }

        public CfInstanceViewModel(CloudFoundryInstance cloudFoundryInstance, IServiceProvider services)
            : base(null, services)
        {
            CloudFoundryInstance = cloudFoundryInstance;
            DisplayText = CloudFoundryInstance.InstanceName;

            LoadingPlaceholder = new PlaceholderViewModel(parent: this, services)
            {
                DisplayText = _loadingMsg,
            };

            EmptyPlaceholder = new PlaceholderViewModel(parent: this, Services)
            {
                DisplayText = _emptyOrgsPlaceholderMsg,
            };
        }

        internal protected override async Task LoadChildren()
        {
            var orgsResponse = await CloudFoundryService.GetOrgsForCfInstanceAsync(CloudFoundryInstance);

            if (orgsResponse.Succeeded)
            {
                if (orgsResponse.Content.Count == 0)
                {
                    var noChildrenList = new ObservableCollection<TreeViewItemViewModel>
                    {
                        EmptyPlaceholder,
                    };

                    Children = noChildrenList;
                    HasEmptyPlaceholder = true;
                }
                else
                {
                    var updatedOrgsList = new ObservableCollection<TreeViewItemViewModel>();
                    foreach (CloudFoundryOrganization org in orgsResponse.Content)
                    {
                        var newOrg = new OrgViewModel(org, Services);
                        updatedOrgsList.Add(newOrg);
                    }

                    Children = updatedOrgsList;
                    HasEmptyPlaceholder = false;
                }

                IsLoading = false;
            }
            else
            {
                IsLoading = false;

                DialogService.DisplayErrorDialog(_getOrgsFailureMsg, orgsResponse.Explanation);

                IsExpanded = false;
            }
        }


        public async Task<ObservableCollection<OrgViewModel>> FetchChildren()
        {
            var newOrgsList = new ObservableCollection<OrgViewModel>();

            var orgsResponse = await CloudFoundryService.GetOrgsForCfInstanceAsync(CloudFoundryInstance);

            if (orgsResponse.Succeeded)
            {
                var orgs = new ObservableCollection<CloudFoundryOrganization>(orgsResponse.Content);

                foreach (CloudFoundryOrganization org in orgs)
                {
                    var newOrg = new OrgViewModel(org, Services);
                    newOrgsList.Add(newOrg);
                }
            }
            else
            {
                DialogService.DisplayErrorDialog(_getOrgsFailureMsg, orgsResponse.Explanation);
            }

            return newOrgsList;
        }
    }

}
