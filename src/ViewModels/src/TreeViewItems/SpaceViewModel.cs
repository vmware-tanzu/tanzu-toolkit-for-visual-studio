using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Tanzu.Toolkit.Models;
using Tanzu.Toolkit.Services.ErrorDialog;

namespace Tanzu.Toolkit.ViewModels
{
    public class SpaceViewModel : TreeViewItemViewModel
    {
        internal const string EmptyAppsPlaceholderMsg = "No apps";
        internal const string LoadingMsg = "Loading apps...";
        internal static readonly string _getAppsFailureMsg = "Unable to load apps.";
        private static IErrorDialog _dialogService;

        public CloudFoundrySpace Space { get; }

        public SpaceViewModel(CloudFoundrySpace space, IServiceProvider services)
            : base(null, services)
        {
            _dialogService = services.GetRequiredService<IErrorDialog>();
            Space = space;
            DisplayText = Space.SpaceName;

            LoadingPlaceholder = new PlaceholderViewModel(parent: this, services)
            {
                DisplayText = LoadingMsg,
            };

            EmptyPlaceholder = new PlaceholderViewModel(parent: this, Services)
            {
                DisplayText = EmptyAppsPlaceholderMsg,
            };
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
                _dialogService.DisplayErrorDialog(_getAppsFailureMsg, appsResult.Explanation);
            }

            return newAppsList;
        }

        protected internal override async Task LoadChildren()
        {
            var appsResult = await CloudFoundryService.GetAppsForSpaceAsync(Space);

            if (appsResult.Succeeded)
            {
                if (appsResult.Content.Count == 0)
                {
                    var noChildrenList = new ObservableCollection<TreeViewItemViewModel>
                    {
                        EmptyPlaceholder,
                    };

                    Children = noChildrenList;
                    HasEmptyPlaceholder = true;
                }
                else
                {
                    var updatedAppsList = new ObservableCollection<TreeViewItemViewModel>();
                    foreach (CloudFoundryApp app in appsResult.Content)
                    {
                        updatedAppsList.Add(new AppViewModel(app, Services));
                    }

                    Children = updatedAppsList;
                    HasEmptyPlaceholder = false;
                }

                IsLoading = false;
            }
            else
            {
                IsLoading = false;

                _dialogService.DisplayErrorDialog(_getAppsFailureMsg, appsResult.Explanation);

                IsExpanded = false;
            }
        }
    }
}
