﻿using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Tanzu.Toolkit.Models;

namespace Tanzu.Toolkit.ViewModels
{
    public class SpaceViewModel : TreeViewItemViewModel
    {
        internal const string EmptyAppsPlaceholderMsg = "No apps";
        internal const string LoadingMsg = "Loading apps...";
        internal static readonly string _getAppsFailureMsg = "Unable to load apps.";

        public CloudFoundrySpace Space { get; }

        public SpaceViewModel(CloudFoundrySpace space, IServiceProvider services, bool expanded = false)
            : base(null, services, expanded: expanded)
        {
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
                DialogService.DisplayErrorDialog(_getAppsFailureMsg, appsResult.Explanation);
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

                DialogService.DisplayErrorDialog(_getAppsFailureMsg, appsResult.Explanation);

                IsExpanded = false;
            }
        }

        public override async Task RefreshChildren()
        {
            await LoadChildren();
        }
    }
}
