using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
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

        private volatile int _updatesInProgress = 0;
        private readonly object _loadingLock = new object();

        public CloudFoundryOrganization Org { get; }

        public OrgViewModel(CloudFoundryOrganization org, CfInstanceViewModel parentCfInstanceViewModel, ITasExplorerViewModel parentTasExplorer, IServiceProvider services, bool expanded = false)
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

        protected internal override async Task UpdateAllChildren()
        {
            if (IsExpanded && !IsLoading)
            {
                lock (_loadingLock)
                {
                    _updatesInProgress += 1;
                }

                if (_updatesInProgress == 1)
                {
                    IsLoading = true;
                    try
                    {
                        var spacesResponse = await ParentTasExplorer.TasConnection.CfClient.GetSpacesForOrgAsync(Org);
                        if (spacesResponse.Succeeded)
                        {
                            // make a working copy of children to avoid System.InvalidOperationException:
                            // "Collection was modified; enumeration operation may not execute."
                            var originalChildren = Children.ToList();

                            var removalTasks = new List<Task>();
                            var additionTasks = new List<Task>();
                            var updateChildrenTasks = new List<Task>();

                            var freshSpaces = new ObservableCollection<CloudFoundrySpace>(spacesResponse.Content);
                            if (freshSpaces.Count < 1)
                            {
                                removalTasks.AddRange(originalChildren.Select(child => ThreadingService.RemoveItemFromCollectionOnUiThreadAsync(Children, child)));
                                additionTasks.Add(ThreadingService.AddItemToCollectionOnUiThreadAsync(Children, EmptyPlaceholder));
                            }
                            else
                            {
                                // identify stale spaces to remove
                                foreach (var priorChild in originalChildren)
                                {
                                    if (priorChild is PlaceholderViewModel)
                                    {
                                        removalTasks.Add(ThreadingService.RemoveItemFromCollectionOnUiThreadAsync(Children, priorChild));
                                    }
                                    else if (priorChild is SpaceViewModel priorSpace)
                                    {
                                        var spaceStillExists = freshSpaces.Any(o => o is CloudFoundrySpace freshSpace && freshSpace != null && freshSpace.SpaceId == priorSpace.Space.SpaceId);
                                        if (!spaceStillExists)
                                        {
                                            removalTasks.Add(ThreadingService.RemoveItemFromCollectionOnUiThreadAsync(Children, priorSpace));
                                        }
                                    }
                                }

                                // identify new spaces to add
                                foreach (var freshSpace in freshSpaces)
                                {
                                    var spaceAlreadyExists = originalChildren.Any(child => child is SpaceViewModel extantSpace && extantSpace.Space.SpaceId == freshSpace.SpaceId);
                                    if (!spaceAlreadyExists)
                                    {
                                        var newSpace = new SpaceViewModel(freshSpace, this, ParentTasExplorer, Services, expanded: false);
                                        additionTasks.Add(ThreadingService.AddItemToCollectionOnUiThreadAsync(Children, newSpace));
                                    }
                                }
                            }

                            await Task.WhenAll(removalTasks);
                            await Task.WhenAll(additionTasks);

                            // update children
                            foreach (var updatedChild in Children)
                            {
                                if (updatedChild is SpaceViewModel space)
                                {
                                    updateChildrenTasks.Add(ThreadingService.StartBackgroundTask(space.UpdateAllChildren));
                                }
                            }
                            await Task.WhenAll(updateChildrenTasks);
                        }
                        else if (spacesResponse.FailureType == Toolkit.Services.FailureType.InvalidRefreshToken)
                        {
                            IsExpanded = false;
                            ParentTasExplorer.AuthenticationRequired = true;
                        }
                        else
                        {
                            Logger.Error("OrgViewModel failed to load spaces: {OrgViewModelLoadingException}", spacesResponse.Explanation);
                            IsExpanded = false;
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.Error("Caught exception trying to load spaces in OrgViewModel: {OrgViewModelLoadingException}", ex);
                        _dialogService.DisplayWarningDialog(_getSpacesFailureMsg, "Something went wrong while loading spaces; try disconnecting & logging in again.\nIf this issue persists, please contact dotnetdevx@groups.vmware.com");
                    }
                    finally
                    {
                        IsLoading = false;
                        lock (_loadingLock)
                        {
                            _updatesInProgress = 0;
                        }
                    }
                }
            }
        }
    }
}
