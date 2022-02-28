using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Tanzu.Toolkit.Models;
using Tanzu.Toolkit.Services.ErrorDialog;

namespace Tanzu.Toolkit.ViewModels
{
    public class OrgViewModel : TreeViewItemViewModel
    {
        /* ERROR MESSAGE CONSTANTS */
        internal static readonly string _emptySpacesPlaceholderMsg = "No spaces found";
        internal static readonly string _loadingMsg = "Loading spaces...";
        internal static readonly string _getSpacesFailureMsg = "Unable to load spaces.";
        private readonly IErrorDialog _dialogService;

        private volatile bool _isRefreshing = false;
        private readonly object _threadLock = new object();

        /// <summary>
        /// A thread-safe indicator of whether or not this <see cref="OrgViewModel"/> is in the process of updating its children.
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

        public CloudFoundryOrganization Org { get; }

        public OrgViewModel(CloudFoundryOrganization org, CfInstanceViewModel parentCfInstanceViewModel, TasExplorerViewModel parentTasExplorer, IServiceProvider services, bool expanded = false)
            : base(parentCfInstanceViewModel, parentTasExplorer, services, expanded: expanded)
        {
            _dialogService = services.GetRequiredService<IErrorDialog>();

            Org = org;
            DisplayText = Org.OrgName;

            LoadingPlaceholder = new PlaceholderViewModel(parent: this, services)
            {
                DisplayText = _loadingMsg,
            };

            EmptyPlaceholder = new PlaceholderViewModel(parent: this, Services)
            {
                DisplayText = _emptySpacesPlaceholderMsg,
            };
        }

        public async Task<ObservableCollection<SpaceViewModel>> FetchChildren()
        {
            var newSpacesList = new ObservableCollection<SpaceViewModel>();

            var spacesResponse = await CloudFoundryService.GetSpacesForOrgAsync(Org);

            if (spacesResponse.Succeeded)
            {
                var spaces = new ObservableCollection<CloudFoundrySpace>(spacesResponse.Content);

                foreach (CloudFoundrySpace space in spaces)
                {
                    var newSpace = new SpaceViewModel(space, this, ParentTasExplorer, Services);
                    newSpacesList.Add(newSpace);
                }
            }
            else if (spacesResponse.FailureType == Toolkit.Services.FailureType.InvalidRefreshToken)
            {
                Parent.IsExpanded = false;
                ParentTasExplorer.AuthenticationRequired = true;
            }
            else
            {
                _dialogService.DisplayErrorDialog(_getSpacesFailureMsg, spacesResponse.Explanation);
            }

            return newSpacesList;
        }

        protected internal override async Task LoadChildren()
        {
            var spacesResponse = await CloudFoundryService.GetSpacesForOrgAsync(Org);

            if (spacesResponse.Succeeded)
            {
                if (spacesResponse.Content.Count == 0)
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
                    var updatedSpacesList = new ObservableCollection<TreeViewItemViewModel>();
                    foreach (CloudFoundrySpace space in spacesResponse.Content)
                    {
                        var newSpace = new SpaceViewModel(space, this, ParentTasExplorer, Services);
                        updatedSpacesList.Add(newSpace);
                    }

                    Children = updatedSpacesList;
                    HasEmptyPlaceholder = false;
                }

                IsLoading = false;
            }
            else
            {
                IsLoading = false;

                _dialogService.DisplayWarningDialog(_getSpacesFailureMsg, spacesResponse.Explanation);

                IsExpanded = false;
            }
        }

        public override async Task RefreshChildren()
        {
            if (!IsRefreshing)
            {
                IsRefreshing = true;

                var freshSpaces = await FetchChildren();

                RemoveNonexistentSpaces(freshSpaces);
                AddNewSpaces(freshSpaces);

                if (Children.Count == 0)
                {
                    UiDispatcherService.RunOnUiThread(() => Children.Add(EmptyPlaceholder));
                }
                else if (Children.Count > 1 && HasEmptyPlaceholder)
                {
                    UiDispatcherService.RunOnUiThread(() => Children.Remove(EmptyPlaceholder));
                }
             
                IsRefreshing = false;
            }
        }

        /// <summary>
        /// Add any svms to Children which are in freshSpaces but not in Children.
        /// </summary>
        /// <param name="ovm"></param>
        /// <param name="freshSpaces"></param>
        private void AddNewSpaces(ObservableCollection<SpaceViewModel> freshSpaces)
        {
            foreach (SpaceViewModel newSVM in freshSpaces)
            {
                if (newSVM != null)
                {
                    bool spaceInChildren = Children.Any(svm =>
                    {
                        var oldSVM = svm as SpaceViewModel;
                        return oldSVM != null && oldSVM.Space.SpaceId == newSVM.Space.SpaceId;
                    });

                    if (!spaceInChildren)
                    {
                        UiDispatcherService.RunOnUiThread(() => Children.Add(newSVM));
                    }
                }
            }
        }

        /// <summary>
        /// Remove all svms from Children which don't appear in freshSpaces.
        /// </summary>
        /// <param name="ovm"></param>
        /// <param name="freshSpaces"></param>
        private void RemoveNonexistentSpaces(ObservableCollection<SpaceViewModel> freshSpaces)
        {
            var spacesToRemove = new ObservableCollection<SpaceViewModel>();

            foreach (TreeViewItemViewModel priorChild in Children)
            {
                if (priorChild is SpaceViewModel oldSVM)
                {
                    bool spaceStillExists = freshSpaces.Any(svm => svm != null && svm.Space.SpaceId == oldSVM.Space.SpaceId);

                    if (!spaceStillExists)
                    {
                        spacesToRemove.Add(oldSVM);
                    }
                }
            }

            foreach (SpaceViewModel svm in spacesToRemove)
            {
                UiDispatcherService.RunOnUiThread(() => Children.Remove(svm));
            }
        }

    }
}
