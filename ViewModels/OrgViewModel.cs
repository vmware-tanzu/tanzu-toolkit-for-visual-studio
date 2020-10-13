using System;
using TanzuForVS.Models;

namespace TanzuForVS.ViewModels
{
    public class OrgViewModel : TreeViewItemViewModel
    {
        readonly CloudFoundryOrganization _org;

        public OrgViewModel(CloudFoundryOrganization org, IServiceProvider services)
            : base(null, true, services)
        {
            _org = org;
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

    }
}
