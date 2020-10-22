using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using TanzuForVS.Models;

namespace TanzuForVS.ViewModels
{
    public class OrgViewModel : TreeViewItemViewModel
    {
        readonly CloudFoundryOrganization _org;
        readonly string _target;
        readonly string _token;

        public OrgViewModel(CloudFoundryOrganization org, string apiAddress, string accessToken, IServiceProvider services)
            : base(null, services)
        {
            _org = org;
            _target = apiAddress;
            _token = accessToken;

            this.DisplayText = _org.OrgName;
        }

        protected override async Task LoadChildren()
        {
            var spaces = await CloudFoundryService.GetSpacesAsync(_target, _token, _org.OrgId);

            if (spaces.Count == 0) DisplayText += " (no spaces)";

            var updatedSpacesList = new ObservableCollection<TreeViewItemViewModel>();
            foreach (CloudFoundrySpace space in spaces)
            {
                updatedSpacesList.Add(new SpaceViewModel(new CloudFoundrySpace(space.SpaceName, space.SpaceId), _target, _token, Services));
            }

            Children = updatedSpacesList;
        }
    }

}
