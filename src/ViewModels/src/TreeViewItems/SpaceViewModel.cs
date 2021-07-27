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
        private readonly IErrorDialog _dialogService;

        private volatile bool _isRefreshing = false;
        private readonly object _threadLock = new object();

        /// <summary>
        /// A thread-safe indicator of whether or not this <see cref="SpaceViewModel"/> is in the process of updating its children.
        /// </summary>
        public bool IsRefreshing
        {
            get
            {
                lock (_threadLock)
                {
                    return _isRefreshing;
                }
            }

            private set
            {
                lock (_threadLock)
                {
                    _isRefreshing = value;
                }

                RaisePropertyChangedEvent("IsRefreshing");
            }
        }

        public CloudFoundrySpace Space { get; }

        public SpaceViewModel(CloudFoundrySpace space, OrgViewModel parentOrgViewModel, TasExplorerViewModel parentTasExplorer, IServiceProvider services, bool expanded = false)
            : base(parentOrgViewModel, parentTasExplorer, services, expanded: expanded)
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
            else if (appsResult.FailureType == Toolkit.Services.FailureType.InvalidRefreshToken)
            {
                Parent.Parent.IsExpanded = false;
                ParentTasExplorer.AuthenticationRequired = true;
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

        public override async Task RefreshChildren()
        {
            if (!IsRefreshing)
            {
                IsRefreshing = true;

                var freshApps = await FetchChildren();
                ReplaceChildren(freshApps);

                IsRefreshing = false;
            }
        }

        private void ReplaceChildren(ObservableCollection<AppViewModel> freshApps)
        {
            UiDispatcherService.RunOnUiThread(() => Children.Clear());
            foreach (TreeViewItemViewModel avm in freshApps)
            {
                UiDispatcherService.RunOnUiThread(() => Children.Add(avm));
            }

            if (Children.Count == 0)
            {
                UiDispatcherService.RunOnUiThread(() => Children.Add(EmptyPlaceholder));
            }
            else if (Children.Count > 1 && HasEmptyPlaceholder)
            {
                UiDispatcherService.RunOnUiThread(() => Children.Remove(EmptyPlaceholder));
            }
        }
    }
}
