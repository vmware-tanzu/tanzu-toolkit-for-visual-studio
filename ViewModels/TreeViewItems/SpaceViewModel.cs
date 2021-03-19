using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Tanzu.Toolkit.VisualStudio.Models;

namespace Tanzu.Toolkit.VisualStudio.ViewModels
{
    public class SpaceViewModel : TreeViewItemViewModel
    {
        internal const string emptyAppsPlaceholderMsg = "No apps";
        internal const string loadingMsg = "Loading apps...";
        internal static readonly string _getAppsFailureMsg = "Unable to load apps.";


        public CloudFoundrySpace Space { get; }

        public SpaceViewModel(CloudFoundrySpace space, IServiceProvider services)
            : base(null, services)
        {
            Space = space;
            DisplayText = Space.SpaceName;

            LoadingPlaceholder = new PlaceholderViewModel(parent: this, services)
            {
                DisplayText = loadingMsg
            };
        }

        internal protected override async Task LoadChildren()
        {
            var appsResult = await CloudFoundryService.GetAppsForSpaceAsync(Space);

            if (appsResult.Succeeded)
            {

                if (appsResult.Content.Count == 0)
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
                    foreach (CloudFoundryApp app in appsResult.Content) updatedAppsList.Add(new AppViewModel(app, Services));

                    Children = updatedAppsList;
                }

                IsLoading = false;
            }

            else
            {
                IsLoading = false;

                DialogService.DisplayErrorDialog(_getAppsFailureMsg, appsResult.Explanation);

                IsExpanded = false;
            }

        }

        public async Task<ObservableCollection<AppViewModel>> FetchChildren()
        {
            var newAppsList = new ObservableCollection<AppViewModel>();

            var appsResult = await CloudFoundryService.GetAppsForSpaceAsync(Space);

            if (appsResult.Succeeded)
            {
                foreach (CloudFoundryApp app in appsResult.Content)
                {
                    var newOrg = new AppViewModel(app, Services);
                    newAppsList.Add(newOrg);
                }
            }
            else
            {
                DialogService.DisplayErrorDialog(_getAppsFailureMsg, appsResult.Explanation);
            }

            return newAppsList;
        }
    }
}
