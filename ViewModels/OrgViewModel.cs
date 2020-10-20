using System;
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
            : base(null, true, services)
        {
            _org = org;
            _target = apiAddress;
            _token = accessToken;
        }

        public string OrgName
        {
            get
            {
                return _org.OrgName;
            }
            set
            {
                _org.OrgName = value;
                RaisePropertyChangedEvent("OrgName");
            }
        }

        protected override async Task LoadChildren()
        {
            var spaceNames = await CloudFoundryService.GetSpaceNamesAsync(_target, _token, _org.OrgId);
            foreach (string spaceName in spaceNames) base.Children.Add(new SpaceViewModel(new CloudFoundrySpace(spaceName), Services));
        }
    }
    
}
