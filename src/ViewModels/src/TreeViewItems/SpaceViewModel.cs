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
        internal const string _emptyAppsPlaceholderMsg = "No apps found";
        internal const string _loadingMsg = "Loading apps...";
        internal static readonly string _getAppsFailureMsg = "Unable to load apps.";
        private readonly IErrorDialog _dialogService;
        private volatile int _updatesInProgress = 0;
        private readonly object _loadingLock = new object();

        public CloudFoundrySpace Space { get; }

        public SpaceViewModel(CloudFoundrySpace space, OrgViewModel parentOrgViewModel, ITanzuExplorerViewModel parentTanzuExplorer, IServiceProvider services, bool expanded = false)
            : base(parentOrgViewModel, parentTanzuExplorer, services, expanded: expanded)
        {
            _dialogService = services.GetRequiredService<IErrorDialog>();
            Space = space;
            DisplayText = Space.SpaceName;

            LoadingPlaceholder = new PlaceholderViewModel(parent: this, services) { DisplayText = _loadingMsg, };

            EmptyPlaceholder = new PlaceholderViewModel(parent: this, Services) { DisplayText = _emptyAppsPlaceholderMsg, };
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
                        var appsResponse = await ParentTanzuExplorer.CloudFoundryConnection.CfClient.GetAppsForSpaceAsync(Space);
                        if (appsResponse.Succeeded)
                        {
                            // make a working copy of children to avoid System.InvalidOperationException:
                            // "Collection was modified; enumeration operation may not execute."
                            var originalChildren = Children.ToList();

                            var removalTasks = new List<Task>();
                            var additionTasks = new List<Task>();

                            var freshApps = new ObservableCollection<CloudFoundryApp>(appsResponse.Content);
                            if (freshApps.Count < 1)
                            {
                                foreach (var child in originalChildren)
                                {
                                    removalTasks.Add(ThreadingService.RemoveItemFromCollectionOnUiThreadAsync(Children, child));
                                }

                                additionTasks.Add(ThreadingService.AddItemToCollectionOnUiThreadAsync(Children, EmptyPlaceholder));
                            }
                            else
                            {
                                // identify stale apps to remove
                                foreach (var priorChild in originalChildren)
                                {
                                    if (priorChild is PlaceholderViewModel)
                                    {
                                        removalTasks.Add(ThreadingService.RemoveItemFromCollectionOnUiThreadAsync(Children, priorChild));
                                    }
                                    else if (priorChild is AppViewModel priorApp)
                                    {
                                        var appStillExists = freshApps.Any(o => o is CloudFoundryApp freshApp && freshApp != null && freshApp.AppId == priorApp.App.AppId);
                                        if (!appStillExists)
                                        {
                                            removalTasks.Add(ThreadingService.RemoveItemFromCollectionOnUiThreadAsync(Children, priorApp));
                                        }
                                    }
                                }

                                // identify new apps to add
                                foreach (var freshApp in freshApps)
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
                            }

                            await Task.WhenAll(removalTasks);
                            await Task.WhenAll(additionTasks);
                            foreach (var child in Children)
                            {
                                if (child is AppViewModel app)
                                {
                                    app.RefreshAppState();
                                }
                            }
                        }
                        else if (appsResponse.FailureType == Toolkit.Services.FailureType.InvalidRefreshToken)
                        {
                            IsExpanded = false;
                            ParentTanzuExplorer.AuthenticationRequired = true;
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
                        _dialogService.DisplayWarningDialog(_getAppsFailureMsg, "Something went wrong while loading apps; try disconnecting & logging in again.");
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