using System;
using System.Threading.Tasks;
using TanzuForVS.Models;

namespace TanzuForVS.ViewModels
{
    public class CfInstanceViewModel : TreeViewItemViewModel
    {
        readonly CloudFoundryInstance _cloudFoundryInstance;

        public CfInstanceViewModel(CloudFoundryInstance cloudFoundryInstance, IServiceProvider services)
            : base(null, services)
        {
            _cloudFoundryInstance = cloudFoundryInstance;
            this.DisplayText = _cloudFoundryInstance.InstanceName;
        }

        protected override async Task LoadChildren()
        {
            var orgs = await CloudFoundryService.GetOrgsAsync(_cloudFoundryInstance.ApiAddress, _cloudFoundryInstance.AccessToken);
            foreach (CloudFoundryOrganization org in orgs)
            {
                var newOrg = new OrgViewModel(org, _cloudFoundryInstance.ApiAddress, _cloudFoundryInstance.AccessToken, Services);
                base.Children.Add(newOrg);
            }
        }
    }
}
