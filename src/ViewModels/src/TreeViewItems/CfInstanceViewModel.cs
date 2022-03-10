using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Tanzu.Toolkit.Models;
using Tanzu.Toolkit.Services.CloudFoundry;
using Tanzu.Toolkit.Services.ErrorDialog;

namespace Tanzu.Toolkit.ViewModels
{
    public class CfInstanceViewModel : TreeViewItemViewModel
    {
        /* ERROR MESSAGE CONSTANTS */
        internal static readonly string _emptyOrgsPlaceholderMsg = "No orgs found";
        internal static readonly string _loadingMsg = "Loading orgs...";
        internal static readonly string _getOrgsFailureMsg = "Unable to load orgs";
        private readonly IErrorDialog _dialogService;

        private volatile int _updatesInProgress = 0;
        private readonly object _loadingLock = new object();

        public CfInstanceViewModel(CloudFoundryInstance cf, ITasExplorerViewModel parentTasExplorer, IServiceProvider services, bool expanded = false)
            : base(null, parentTasExplorer, services, expanded: expanded)
        {
            _dialogService = services.GetRequiredService<IErrorDialog>();
            CfClient = services.GetRequiredService<ICloudFoundryService>();

            CloudFoundryInstance = cf;
            DisplayText = cf.InstanceName;
            CfClient.ConfigureForCf(cf);

            LoadingPlaceholder = new PlaceholderViewModel(parent: this, services)
            {
                DisplayText = _loadingMsg,
            };

            EmptyPlaceholder = new PlaceholderViewModel(parent: this, Services)
            {
                DisplayText = _emptyOrgsPlaceholderMsg,
            };
        }

        public CloudFoundryInstance CloudFoundryInstance { get; }

        public ICloudFoundryService CfClient { get; private set; }

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
                        var orgsResponse = await CfClient.GetOrgsForCfInstanceAsync(CloudFoundryInstance);
                        if (orgsResponse.Succeeded)
                        {
                            // make a working copy of children to avoid System.InvalidOperationException:
                            // "Collection was modified; enumeration operation may not execute."
                            var originalChildren = Children.ToList();

                            var removalTasks = new List<Task>();
                            var additionTasks = new List<Task>();
                            var updateTasks = new List<Task>();

                            var freshOrgs = new ObservableCollection<CloudFoundryOrganization>(orgsResponse.Content);
                            if (freshOrgs.Count < 1)
                            {
                                foreach (var child in originalChildren)
                                {
                                    removalTasks.Add(ThreadingService.RemoveItemFromCollectionOnUiThreadAsync(Children, child));
                                }
                                additionTasks.Add(ThreadingService.AddItemToCollectionOnUiThreadAsync(Children, EmptyPlaceholder));
                            }
                            else
                            {
                                // remove stale orgs
                                foreach (var priorChild in originalChildren)
                                {
                                    if (priorChild is PlaceholderViewModel)
                                    {
                                        removalTasks.Add(ThreadingService.RemoveItemFromCollectionOnUiThreadAsync(Children, priorChild));
                                    }
                                    else if (priorChild is OrgViewModel priorOrg)
                                    {
                                        var orgStillExists = freshOrgs.Any(o => o is CloudFoundryOrganization freshOrg && freshOrg != null && freshOrg.OrgId == priorOrg.Org.OrgId);
                                        if (!orgStillExists)
                                        {
                                            removalTasks.Add(ThreadingService.RemoveItemFromCollectionOnUiThreadAsync(Children, priorOrg));
                                        }
                                    }
                                }

                                // add new orgs
                                foreach (var freshOrg in freshOrgs)
                                {
                                    var orgAlreadyExists = originalChildren.Any(child => child is OrgViewModel extantOrg && extantOrg.Org.OrgId == freshOrg.OrgId);
                                    if (!orgAlreadyExists)
                                    {
                                        var newOrg = new OrgViewModel(freshOrg, this, ParentTasExplorer, Services, expanded: false);
                                        additionTasks.Add(ThreadingService.AddItemToCollectionOnUiThreadAsync(Children, newOrg));
                                    }
                                }
                            }

                            await Task.WhenAll(removalTasks);
                            await Task.WhenAll(additionTasks);

                            // update children
                            foreach (var child in Children)
                            {
                                if (child is OrgViewModel org)
                                {
                                    updateTasks.Add(ThreadingService.StartBackgroundTask(org.UpdateAllChildren));
                                }
                            }
                            await Task.WhenAll(updateTasks);
                        }
                        else if (orgsResponse.FailureType == Toolkit.Services.FailureType.InvalidRefreshToken)
                        {
                            IsExpanded = false;
                            ParentTasExplorer.AuthenticationRequired = true;
                        }
                        else
                        {
                            Logger.Error("CfInstanceViewModel failed to load orgs: {CfInstanceViewModelLoadingException}", orgsResponse.Explanation);
                            IsExpanded = false;
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.Error("Caught exception trying to load orgs in CfInstanceViewModel: {CfInstanceViewModelLoadingException}", ex);
                        _dialogService.DisplayWarningDialog(_getOrgsFailureMsg, "Something went wrong while loading organizations; try disconnecting & logging in again.\nIf this issue persists, please contact dotnetdevx@groups.vmware.com");
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
