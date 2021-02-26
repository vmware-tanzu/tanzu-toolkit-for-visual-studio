using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Tanzu.Toolkit.VisualStudio.Models;

namespace Tanzu.Toolkit.VisualStudio.ViewModels
{
    public class SpaceViewModel : TreeViewItemViewModel
    {
        public CloudFoundrySpace Space { get; }

        public SpaceViewModel(CloudFoundrySpace space, IServiceProvider services)
            : base(null, services)
        {
            Space = space;
            DisplayText = Space.SpaceName;
        }

        protected override async Task LoadChildren()
        {
            var apps = await CloudFoundryService.GetAppsForSpaceAsync(Space); 

            if (apps.Count == 0 && !DisplayText.Contains("(no apps)")) DisplayText += " (no apps)";

            var updatedAppsList = new ObservableCollection<TreeViewItemViewModel>();
            foreach (CloudFoundryApp app in apps) updatedAppsList.Add(new AppViewModel(app, Services));

            Children = updatedAppsList;
        }

        public async Task<List<AppViewModel>> FetchChildren()
        {
            var newAppsList = new List<AppViewModel>();

            var apps = await CloudFoundryService.GetAppsForSpaceAsync(Space);
            foreach (CloudFoundryApp app in apps)
            {
                var newOrg = new AppViewModel(app, Services);
                newAppsList.Add(newOrg);
            }

            return newAppsList;
        }
    }
}
