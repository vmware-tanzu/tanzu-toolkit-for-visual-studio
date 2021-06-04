using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Tanzu.Toolkit.Models;
using Tanzu.Toolkit.Services.ErrorDialog;

namespace Tanzu.Toolkit.ViewModels
{
    public class CfInstanceViewModel : TreeViewItemViewModel
    {
        /* ERROR MESSAGE CONSTANTS */
        internal static readonly string _emptyOrgsPlaceholderMsg = "No orgs";
        internal static readonly string _loadingMsg = "Loading orgs...";
        internal static readonly string _getOrgsFailureMsg = "Unable to load orgs";
        private static IErrorDialog _dialogService;

        private volatile bool _isRefreshing = false;
        private readonly object _threadLock = new object();

        /// <summary>
        /// A thread-safe indicator of whether or not this <see cref="CfInstanceViewModel"/> is in the process of updating its children.
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

        public CloudFoundryInstance CloudFoundryInstance { get; }

        public CfInstanceViewModel(CloudFoundryInstance cloudFoundryInstance, IServiceProvider services, bool expanded = false)
            : base(null, services, expanded: expanded)
        {
            _dialogService = services.GetRequiredService<IErrorDialog>();
            CloudFoundryInstance = cloudFoundryInstance;
            DisplayText = CloudFoundryInstance.InstanceName;

            LoadingPlaceholder = new PlaceholderViewModel(parent: this, services)
            {
                DisplayText = _loadingMsg,
            };

            EmptyPlaceholder = new PlaceholderViewModel(parent: this, Services)
            {
                DisplayText = _emptyOrgsPlaceholderMsg,
            };
        }

        public async Task<ObservableCollection<OrgViewModel>> FetchChildren()
        {
            var newOrgsList = new ObservableCollection<OrgViewModel>();

            var orgsResponse = await CloudFoundryService.GetOrgsForCfInstanceAsync(CloudFoundryInstance);

            if (orgsResponse.Succeeded)
            {
                var orgs = new ObservableCollection<CloudFoundryOrganization>(orgsResponse.Content);

                foreach (CloudFoundryOrganization org in orgs)
                {
                    var newOrg = new OrgViewModel(org, Services);
                    newOrgsList.Add(newOrg);
                }
            }
            else
            {
                _dialogService.DisplayErrorDialog(_getOrgsFailureMsg, orgsResponse.Explanation);
            }

            return newOrgsList;
        }

        protected internal override async Task LoadChildren()
        {
            var orgsResponse = await CloudFoundryService.GetOrgsForCfInstanceAsync(CloudFoundryInstance);

            if (orgsResponse.Succeeded)
            {
                if (orgsResponse.Content.Count == 0)
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
                    var updatedOrgsList = new ObservableCollection<TreeViewItemViewModel>();
                    foreach (CloudFoundryOrganization org in orgsResponse.Content)
                    {
                        var newOrg = new OrgViewModel(org, Services);
                        updatedOrgsList.Add(newOrg);
                    }

                    Children = updatedOrgsList;
                    HasEmptyPlaceholder = false;
                }

                IsLoading = false;
            }
            else
            {
                IsLoading = false;

                _dialogService.DisplayErrorDialog(_getOrgsFailureMsg, orgsResponse.Explanation);

                IsExpanded = false;
            }
        }

        public override async Task RefreshChildren()
        {
            if (!IsRefreshing)
            {
                IsRefreshing = true;

                var freshOrgs = await FetchChildren();

                RemoveNonexistentOrgs(freshOrgs);
                AddNewOrgs(freshOrgs);

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
        /// add any ovms to cfivm.Children which are in currentOrgs but not in cfivm.Children.
        /// </summary>
        /// <param name="cfivm"></param>
        /// <param name="freshOrgs"></param>
        private void AddNewOrgs(ObservableCollection<OrgViewModel> freshOrgs)
        {
            foreach (OrgViewModel newOVM in freshOrgs)
            {
                if (newOVM != null)
                {
                    bool orgInChildren = Children.Any(ovm =>
                    {
                        var oldOVM = ovm as OrgViewModel;
                        return oldOVM != null && oldOVM.Org.OrgId == newOVM.Org.OrgId;
                    });

                    if (!orgInChildren)
                    {
                        UiDispatcherService.RunOnUiThread(() => Children.Add(newOVM));
                    }
                }
            }
        }

        /// <summary>
        /// remove all ovms from cfivm.Children which don't appear in currentOrgs.
        /// </summary>
        /// <param name="cfivm"></param>
        /// <param name="freshOrgs"></param>
        private void RemoveNonexistentOrgs(ObservableCollection<OrgViewModel> freshOrgs)
        {
            var orgsToRemove = new ObservableCollection<OrgViewModel>();

            foreach (TreeViewItemViewModel priorChild in Children)
            {
                if (priorChild is OrgViewModel oldOVM)
                {
                    bool orgStillExists = freshOrgs.Any(ovm => ovm != null && ovm.Org.OrgId == oldOVM.Org.OrgId);

                    if (!orgStillExists)
                    {
                        orgsToRemove.Add(oldOVM);
                    }
                }
            }

            foreach (OrgViewModel ovm in orgsToRemove)
            {
                UiDispatcherService.RunOnUiThread(() => Children.Remove(ovm));
            }
        }

    }
}
