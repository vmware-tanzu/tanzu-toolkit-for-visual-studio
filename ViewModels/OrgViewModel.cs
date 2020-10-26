using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using TanzuForVS.Models;

namespace TanzuForVS.ViewModels
{
    public class OrgViewModel : TreeViewItemViewModel
    {
        public CloudFoundryOrganization Org { get; }

        public OrgViewModel(CloudFoundryOrganization org, IServiceProvider services)
            : base(null, services)
        {
            Org = org;
            this.DisplayText = Org.OrgName;
        }

        protected override async Task LoadChildren()
        {
            var spaces = await CloudFoundryService.GetSpacesForOrgAsync(Org);

            if (spaces.Count == 0) DisplayText += " (no spaces)";

            var updatedSpacesList = new ObservableCollection<TreeViewItemViewModel>();
            foreach (CloudFoundrySpace space in spaces)
            {
                updatedSpacesList.Add(new SpaceViewModel(new CloudFoundrySpace(space.SpaceName, space.SpaceId, Org), Services));
            }

            Children = updatedSpacesList;
        }
    }

}
