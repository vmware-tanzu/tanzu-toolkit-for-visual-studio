using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Tanzu.Toolkit.VisualStudio.Models;

namespace Tanzu.Toolkit.VisualStudio.ViewModels
{
    public class OrgViewModel : TreeViewItemViewModel
    {
        internal const string emptySpacesPlaceholderMsg = "No spaces";
        internal const string loadingMsg = "Loading spaces...";

        public CloudFoundryOrganization Org { get; }

        public OrgViewModel(CloudFoundryOrganization org, IServiceProvider services)
            : base(null, services)
        {
            Org = org;
            DisplayText = Org.OrgName;

            LoadingPlaceholder = new PlaceholderViewModel(parent: this, services)
            {
                DisplayText = loadingMsg
            };
        }

        internal protected override async Task LoadChildren()
        {
            var spaces = await CloudFoundryService.GetSpacesForOrgAsync(Org);

            if (spaces.Count == 0)
            {
                var noChildrenList = new ObservableCollection<TreeViewItemViewModel>
                {
                    new PlaceholderViewModel(parent: this, Services)
                    {
                        DisplayText = emptySpacesPlaceholderMsg
                    }
                };

                Children = noChildrenList;
            }
            else
            {
                var updatedSpacesList = new ObservableCollection<TreeViewItemViewModel>();
                foreach (CloudFoundrySpace space in spaces)
                {
                    updatedSpacesList.Add(new SpaceViewModel(new CloudFoundrySpace(space.SpaceName, space.SpaceId, Org), Services));
                }

                Children = updatedSpacesList;
            }

            IsLoading = false;
        }

        public async Task<List<SpaceViewModel>> FetchChildren()
        {
            var newSpacesList = new List<SpaceViewModel>();

            var spaces = await CloudFoundryService.GetSpacesForOrgAsync(Org);
            foreach (CloudFoundrySpace space in spaces)
            {
                var newSpace = new SpaceViewModel(space, Services);
                newSpacesList.Add(newSpace);
            }

            return newSpacesList;
        }
    }

}
