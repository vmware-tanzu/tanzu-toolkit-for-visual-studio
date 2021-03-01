using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Tanzu.Toolkit.VisualStudio.Models;

namespace Tanzu.Toolkit.VisualStudio.ViewModels
{
    public class SpaceViewModel : TreeViewItemViewModel
    {
        internal const string emptyAppsPlaceholderMsg = "No apps";
        internal const string loadingMsg = "Loading apps...";

        public CloudFoundrySpace Space { get; }

        public SpaceViewModel(CloudFoundrySpace space, IServiceProvider services)
            : base(null, services)
        {
            Space = space;
            DisplayText = Space.SpaceName;
        }

        protected override async Task LoadChildren()
        {
            Children = new ObservableCollection<TreeViewItemViewModel>
            {
                new PlaceholderViewModel(parent: this, Services)
                {
                    DisplayText = loadingMsg
                }
            };

            var apps = await CloudFoundryService.GetAppsForSpaceAsync(Space);

            if (apps.Count == 0)
            {
                var noChildrenList = new ObservableCollection<TreeViewItemViewModel>
                {
                    new PlaceholderViewModel(parent: this, Services)
                    {
                        DisplayText = emptyAppsPlaceholderMsg
                    }
                };

                Children = noChildrenList;
            }
            else
            {
                var updatedAppsList = new ObservableCollection<TreeViewItemViewModel>();
                foreach (CloudFoundryApp app in apps) updatedAppsList.Add(new AppViewModel(app, Services));

                Children = updatedAppsList;
            }
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
