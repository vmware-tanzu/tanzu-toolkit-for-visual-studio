using System;
using System.Threading.Tasks;
using TanzuForVS.Models;

namespace TanzuForVS.ViewModels
{
    public class CloudFoundryInstanceViewModel : TreeViewItemViewModel
    {
        readonly CloudFoundryInstance _cloudFoundryInstance;

        public CloudFoundryInstanceViewModel(CloudFoundryInstance cloudFoundryInstance, IServiceProvider services)
            : base(null, true, services)
        {
            _cloudFoundryInstance = cloudFoundryInstance;
        }

        public string InstanceName
        {
            get { return _cloudFoundryInstance.InstanceName; }
            set
            {
                _cloudFoundryInstance.InstanceName = value;
                RaisePropertyChangedEvent("InstanceName");
            }
        }

        protected override async Task LoadChildren()
        {
            var orgNames = await CloudFoundryService.GetOrgNamesAsync(_cloudFoundryInstance.ApiAddress, _cloudFoundryInstance.AccessToken);
            foreach (string orgName in orgNames) base.Children.Add(new OrgViewModel(new CloudFoundryOrganization(orgName), Services));
        }
    }
}
