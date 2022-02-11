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
    public class SpaceViewModel : TreeViewItemViewModel
    {
        internal const string EmptyAppsPlaceholderMsg = "No apps";
        internal const string LoadingMsg = "Loading apps...";
        internal static readonly string _getAppsFailureMsg = "Unable to load apps.";
        private readonly IErrorDialog _dialogService;

        private volatile int _updatesInProgress = 0;
        private readonly object _loadingLock = new object();

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
                        var appsResponse = await CloudFoundryService.GetAppsForSpaceAsync(Space);
                        if (appsResponse.Succeeded)
                        {
                            var freshApps = new ObservableCollection<CloudFoundryApp>(appsResponse.Content);
                            if (freshApps.Count < 1)
                            {
                                var replacementTask = ThreadingService.ReplaceCollectionOnUiThreadAsync(Children, new ObservableCollection<TreeViewItemViewModel> { EmptyPlaceholder });
                            }
                            else
                            {
                                // make a working copy of children to avoid System.InvalidOperationException:
                                // "Collection was modified; enumeration operation may not execute."
                                var originalChildren = Children.ToList();

                                // remove stale apps
                                var removalTasks = new List<Task>();
                                foreach (TreeViewItemViewModel priorChild in originalChildren)
                                {
                                    if (priorChild is PlaceholderViewModel)
                                    {
                                        removalTasks.Add(ThreadingService.RemoveItemFromCollectionOnUiThreadAsync(Children, priorChild));
                                    }
                                    else if (priorChild is AppViewModel priorApp)
                                    {
                                        bool appStillExists = freshApps.Any(o => o is CloudFoundryApp freshApp && freshApp != null && freshApp.AppId == priorApp.App.AppId);
                                        if (!appStillExists) removalTasks.Add(ThreadingService.RemoveItemFromCollectionOnUiThreadAsync(Children, priorApp));
                                    }
                                }
                                await Task.WhenAll(removalTasks);

                                // add new apps
                                var additionTasks = new List<Task>();
                                foreach (CloudFoundryApp freshApp in freshApps)
                                {
                                    var appsWithSameId = originalChildren.Where(child => child is AppViewModel extantApp && extantApp.App.AppId == freshApp.AppId);
                                    var numMatchingApps = appsWithSameId.Count();
                                    switch (appsWithSameId.Count())
                                    {
                                        case 0: // no existing apps match fresh app's id; add it
                                            var newApp = new AppViewModel(freshApp, Services);
                                            additionTasks.Add(ThreadingService.AddItemToCollectionOnUiThreadAsync(Children, newApp));
                                            break;
                                        case 1: // found matching app; keep it but update state with fresh info
                                            var extantApp = (AppViewModel)appsWithSameId.First();
                                            extantApp.App.State = freshApp.State;
                                            break;
                                        default: // n < 0 should be impossible & n > 1 means there are n extant apps with the same id
                                            Logger.Error("Space {SpaceName} has {NumMatchingApps} app with id {AppId}", Space.SpaceName, numMatchingApps, freshApp.AppId);
                                            break;
                                    }
                                }
                                await Task.WhenAll(additionTasks);

                                foreach (AppViewModel app in Children) app.RefreshAppState();
                            }
                        }
                        else if (appsResponse.FailureType == Toolkit.Services.FailureType.InvalidRefreshToken)
                        {
                            IsExpanded = false;
                            ParentTasExplorer.AuthenticationRequired = true;
                        }
                        else
                        {
                            Logger.Error("SpaceViewModel failed to load apps: {SpaceViewModelLoadingException}", appsResponse.Explanation);
                            IsExpanded = false;
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.Error("Caught exception trying to load apps in SpaceViewModel: {SpaceViewModelLoadingException}", ex);
                        _dialogService.DisplayErrorDialog(_getAppsFailureMsg, "Something went wrong while loading apps; try disconnecting & logging in again.\nIf this issue persists, please contact dotnetdevx@groups.vmware.com");
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
