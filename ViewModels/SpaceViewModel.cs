using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using TanzuForVS.Models;

namespace TanzuForVS.ViewModels
{
    public class SpaceViewModel : TreeViewItemViewModel
    {
        readonly CloudFoundrySpace _space;
        readonly string _target;
        readonly string _token;

        public SpaceViewModel(CloudFoundrySpace space, string apiAddress, string accessToken, IServiceProvider services)
            : base(null, services)
        {
            _space = space;
            _target = apiAddress;
            _token = accessToken;

            this.DisplayText = _space.SpaceName;
        }
        protected override async Task LoadChildren()
        {
            var apps = await CloudFoundryService.GetAppsAsync(_target, _token, _space.SpaceId);

            if (apps.Count == 0) DisplayText += " (no apps)";

            var updatedAppsList = new ObservableCollection<TreeViewItemViewModel>();
            foreach (CloudFoundryApp app in apps) updatedAppsList.Add(new AppViewModel(app, Services));

            Children = updatedAppsList;
        }
    }
}
