using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using TanzuForVS.Models;

namespace TanzuForVS.ViewModels
{
    public class SpaceViewModel : TreeViewItemViewModel
    {
        public CloudFoundrySpace Space { get; }

        public SpaceViewModel(CloudFoundrySpace space, IServiceProvider services)
            : base(null, services)
        {
            Space = space;
            this.DisplayText = Space.SpaceName;
        }

        protected override async Task LoadChildren()
        {
            var apps = await CloudFoundryService.GetAppsForSpaceAsync(Space);
            if (apps.Count == 0) DisplayText += " (no apps)";

            var updatedAppsList = new ObservableCollection<TreeViewItemViewModel>();
            foreach (CloudFoundryApp app in apps) updatedAppsList.Add(new AppViewModel(app, Services));

            Children = updatedAppsList;
        }
    }
}
