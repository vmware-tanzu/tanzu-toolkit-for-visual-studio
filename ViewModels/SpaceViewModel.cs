using System;
using TanzuForVS.Models;

namespace TanzuForVS.ViewModels
{
    public class SpaceViewModel : TreeViewItemViewModel
    {
        readonly CloudFoundrySpace _space;

        public SpaceViewModel(CloudFoundrySpace space, IServiceProvider services)
            : base(null, true, services)
        {
            _space = space;
            this.DisplayText = _space.SpaceName;
        }
    }
}
