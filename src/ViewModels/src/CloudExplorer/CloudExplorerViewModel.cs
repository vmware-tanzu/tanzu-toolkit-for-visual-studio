using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Tanzu.Toolkit.Models;

namespace Tanzu.Toolkit.ViewModels
{
    public class CloudExplorerViewModel : AbstractViewModel, ICloudExplorerViewModel
    {
        internal static readonly string _stopAppErrorMsg = "Encountered an error while stopping app";
        internal static readonly string _startAppErrorMsg = "Encountered an error while starting app";
        internal static readonly string _deleteAppErrorMsg = "Encountered an error while deleting app";

        private bool _hasCloudTargets;
        private ObservableCollection<CfInstanceViewModel> _cfs;
        private readonly IServiceProvider _services;

        public CloudExplorerViewModel(IServiceProvider services)
            : base(services)
        {
            _services = services;
            _cfs = new ObservableCollection<CfInstanceViewModel>();
            HasCloudTargets = CloudFoundryService.CloudFoundryInstances.Keys.Count > 0;
        }

        public ObservableCollection<CfInstanceViewModel> CloudFoundryList
        {
            get => _cfs;

            set
            {
                _cfs = value;
                RaisePropertyChangedEvent("CloudFoundryList");
            }
        }

        public bool HasCloudTargets
        {
            get => _hasCloudTargets;

            set
            {
                _hasCloudTargets = value;
                RaisePropertyChangedEvent("HasCloudTargets");
            }
        }

        public bool CanOpenLoginView(object arg)
        {
            return true;
        }

        public bool CanStopCfApp(object arg)
        {
            return true;
        }

        public bool CanStartCfApp(object arg)
        {
            return true;
        }

        public bool CanDeleteCfApp(object arg)
        {
            return true;
        }

        public bool CanRefreshCfInstance(object arg)
        {
            return true;
        }

        public bool CanRefreshOrg(object arg)
        {
            return true;
        }

        public bool CanRefreshSpace(object arg)
        {
            return true;
        }

        public bool CanRefreshApp(object arg)
        {
            return true;
        }

        public bool CanRefreshAllCloudConnections(object arg)
        {
            return true;
        }

        public bool CanRemoveCloudConnecion(object arg)
        {
            return true;
        }

        public bool CanDisplayRecentAppLogs(object arg)
        {
            return true;
        }

        public void OpenLoginView(object parent)
        {
            if (CloudFoundryService.CloudFoundryInstances.Count > 0)
            {
                var errorTitle = "Unable to add more TAS connections.";
                var errorMsg = "This version of Tanzu Toolkit for Visual Studio only supports 1 cloud connection at a time; multi-cloud connections will be supported in the future.";
                errorMsg += System.Environment.NewLine + "If you want to connect to a different cloud, please delete this one by right-clicking on it in the Cloud Explorer & re-connecting to a new one.";

                DialogService.DisplayErrorDialog(errorTitle, errorMsg);
            }
            else
            {
                DialogService.ShowDialog(typeof(AddCloudDialogViewModel).Name);

                UpdateCloudFoundryInstances();
            }
        }

        public async Task StopCfApp(object app)
        {
            if (app is CloudFoundryApp cfApp)
            {
                var stopResult = await CloudFoundryService.StopAppAsync(cfApp);
                if (!stopResult.Succeeded)
                {
                    Logger.Error(_stopAppErrorMsg + " {AppName}. {StopResult}", cfApp.AppName, stopResult.ToString());
                    DialogService.DisplayErrorDialog($"{_stopAppErrorMsg} {cfApp.AppName}.", stopResult.Explanation);
                }
            }
        }

        public async Task StartCfApp(object app)
        {
            if (app is CloudFoundryApp cfApp)
            {
                var startResult = await CloudFoundryService.StartAppAsync(cfApp);
                if (!startResult.Succeeded)
                {
                    Logger.Error(_startAppErrorMsg + " {AppName}. {StartResult}", cfApp.AppName, startResult.ToString());
                    DialogService.DisplayErrorDialog($"{_startAppErrorMsg} {cfApp.AppName}.", startResult.Explanation);
                }
            }
        }

        public async Task DeleteCfApp(object app)
        {
            if (app is CloudFoundryApp cfApp)
            {
                var deleteResult = await CloudFoundryService.DeleteAppAsync(cfApp);
                if (!deleteResult.Succeeded)
                {
                    Logger.Error(_deleteAppErrorMsg + " {AppName}. {DeleteResult}", cfApp.AppName, deleteResult.ToString());
                    DialogService.DisplayErrorDialog($"{_deleteAppErrorMsg} {cfApp.AppName}.", deleteResult.Explanation);
                }
            }
        }

        public async Task DisplayRecentAppLogs(object app)
        {
            if (app is CloudFoundryApp cfApp)
            {
                var recentLogsResult = await CloudFoundryService.GetRecentLogs(cfApp);
                if (!recentLogsResult.Succeeded)
                {
                    Logger.Error($"Unable to retrieve recent logs for {cfApp.AppName}. {recentLogsResult.Explanation}. {recentLogsResult.CmdDetails}");
                    DialogService.DisplayErrorDialog($"Unable to retrieve recent logs for {cfApp.AppName}.", recentLogsResult.Explanation);
                }
                else
                {
                    IView outputView = ViewLocatorService.NavigateTo(nameof(OutputViewModel)) as IView;
                    var outputViewModel = outputView?.ViewModel as IOutputViewModel;

                    outputView.Show();

                    outputViewModel.AppendLine(recentLogsResult.Content);
                }
            }
            else
            {
                Logger.Error($"CloudExplorerViewModel.GetRecentAppLogs received expected argument 'app' to be of type '{typeof(CloudFoundryApp)}', but instead received type '{app.GetType()}'.");
            }
        }

        public async Task RefreshCfInstance(object cfInstanceViewModel)
        {
            if (cfInstanceViewModel is CfInstanceViewModel cfivm)
            {
                var currentOrgs = await cfivm.FetchChildren();

                RemoveNonexistentOrgs(cfivm, currentOrgs);
                AddNewOrgs(cfivm, currentOrgs);

                if (cfivm.Children.Count == 0)
                {
                    cfivm.Children.Add(cfivm.EmptyPlaceholder);
                }
                else if (cfivm.Children.Count > 1 && cfivm.HasEmptyPlaceholder)
                {
                    cfivm.Children.Remove(cfivm.EmptyPlaceholder);
                }
            }
        }

        public async Task RefreshOrg(object orgViewModel)
        {
            if (orgViewModel is OrgViewModel ovm)
            {
                var currentSpaces = await ovm.FetchChildren();

                RemoveNonexistentSpaces(ovm, currentSpaces);
                AddNewSpaces(ovm, currentSpaces);

                if (ovm.Children.Count == 0)
                {
                    ovm.Children.Add(ovm.EmptyPlaceholder);
                }
                else if (ovm.Children.Count > 1 && ovm.HasEmptyPlaceholder)
                {
                    ovm.Children.Remove(ovm.EmptyPlaceholder);
                }
            }
        }

        public async Task RefreshSpace(object spaceViewModel)
        {
            if (spaceViewModel is SpaceViewModel svm)
            {
                var currentApps = await svm.FetchChildren();

                RemoveNonexistentApps(svm, currentApps);
                AddNewApps(svm, currentApps);

                if (svm.Children.Count == 0)
                {
                    svm.Children.Add(svm.EmptyPlaceholder);
                }
                else if (svm.Children.Count > 1 && svm.HasEmptyPlaceholder)
                {
                    svm.Children.Remove(svm.EmptyPlaceholder);
                }
            }
        }

        public void RefreshApp(object appViewModel)
        {
            if (appViewModel is AppViewModel avm)
            {
                avm.SignalIsStoppedChanged();
            }
        }

        public async Task RefreshAllCloudConnections(object arg)
        {
            // record original items before refreshing starts to
            // avoid unnecessary LoadChildren calls for *new* items
            var initalIds = new List<string>();

            foreach (CfInstanceViewModel cfivm in CloudFoundryList)
            {
                initalIds.Add(cfivm.CloudFoundryInstance.InstanceId);

                foreach (TreeViewItemViewModel cfChild in cfivm.Children)
                {
                    if (cfChild is OrgViewModel ovm)
                    {
                        initalIds.Add(ovm.Org.OrgId);

                        foreach (TreeViewItemViewModel orgChild in ovm.Children)
                        {
                            if (orgChild is SpaceViewModel svm)
                            {
                                initalIds.Add(svm.Space.SpaceId);
                            }
                        }
                    }
                }
            }

            // before refreshing each cf instance, check the Model's record of Cloud
            // connections & make sure `CloudFoundryList` matches those values
            SyncCloudFoundryList();

            foreach (CfInstanceViewModel cfivm in CloudFoundryList)
            {
                bool cfNotNew = initalIds.Contains(cfivm.CloudFoundryInstance.InstanceId);

                if (cfNotNew)
                {
                    await RefreshCfInstance(cfivm);

                    foreach (TreeViewItemViewModel cfChild in cfivm.Children)
                    {
                        if (cfChild is OrgViewModel ovm && initalIds.Contains(ovm.Org.OrgId))
                        {
                            await RefreshOrg(ovm);

                            foreach (TreeViewItemViewModel orgChild in ovm.Children)
                            {
                                if (orgChild is SpaceViewModel svm && initalIds.Contains(svm.Space.SpaceId))
                                {
                                    await RefreshSpace(svm);

                                    foreach (TreeViewItemViewModel spaceChild in svm.Children)
                                    {
                                        if (spaceChild is AppViewModel avm)
                                        {
                                            RefreshApp(avm);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        public void RemoveCloudConnection(object arg)
        {
            if (arg is CfInstanceViewModel cloudConnection)
            {
                CloudFoundryService.RemoveCloudFoundryInstance(cloudConnection.DisplayText);
                SyncCloudFoundryList();
            }
        }

        /// <summary>
        /// Update CloudFoundryList with values from the Model's record of Cloud instances
        /// Do not re-assign CloudFoundryList (avoid raising property changed event).
        /// </summary>
        private void SyncCloudFoundryList()
        {
            var loggedInCfs = new ObservableCollection<CloudFoundryInstance>(CloudFoundryService.CloudFoundryInstances.Values);
            var updatedCfInstanceViewModelList = new ObservableCollection<CfInstanceViewModel>();
            foreach (CloudFoundryInstance cf in loggedInCfs)
            {
                updatedCfInstanceViewModelList.Add(new CfInstanceViewModel(cf, _services));
            }

            RemoveNonexistentCfsFromCloudFoundryList(updatedCfInstanceViewModelList);
            AddNewCfsToCloudFoundryList(updatedCfInstanceViewModelList);

            HasCloudTargets = CloudFoundryList.Count > 0;
        }

        private void AddNewCfsToCloudFoundryList(ObservableCollection<CfInstanceViewModel> updatedCfInstanceViewModelList)
        {
            foreach (CfInstanceViewModel newCFIVM in updatedCfInstanceViewModelList)
            {
                // if newCFIVM isn't in CloudFoundryList, add it
                if (newCFIVM != null)
                {
                    bool cfWasAlreadyInCloudFoundryList = CloudFoundryList.Any(oldCf =>
                    {
                        return oldCf != null
                            && oldCf.CloudFoundryInstance.ApiAddress == newCFIVM.CloudFoundryInstance.ApiAddress
                            && oldCf.CloudFoundryInstance.InstanceName == newCFIVM.CloudFoundryInstance.InstanceName;
                    });

                    if (!cfWasAlreadyInCloudFoundryList)
                    {
                        CloudFoundryList.Add(newCFIVM);
                    }
                }
            }
        }

        private void RemoveNonexistentCfsFromCloudFoundryList(ObservableCollection<CfInstanceViewModel> updatedCfInstanceViewModelList)
        {
            var cfsToRemove = new ObservableCollection<CfInstanceViewModel>();

            foreach (CfInstanceViewModel oldCFIVM in CloudFoundryList)
            {
                // if oldCFIVM isn't in updatedCfInstanceViewModelList, remove it from CloudFoundryList
                if (oldCFIVM != null)
                {
                    bool cfConnectionStillExists = updatedCfInstanceViewModelList.Any(cfivm => cfivm != null
                        && cfivm.CloudFoundryInstance.ApiAddress == oldCFIVM.CloudFoundryInstance.ApiAddress);

                    if (!cfConnectionStillExists)
                    {
                        cfsToRemove.Add(oldCFIVM);
                    }
                }
            }

            foreach (CfInstanceViewModel cfivm in cfsToRemove)
            {
                CloudFoundryList.Remove(cfivm);
            }
        }

        /// <summary>
        /// Add any avms to svm.Children which are in currentApps but not in svm.Children.
        /// </summary>
        /// <param name="svm"></param>
        /// <param name="currentApps"></param>
        private static void AddNewApps(SpaceViewModel svm, ObservableCollection<AppViewModel> currentApps)
        {
            foreach (AppViewModel newAVM in currentApps)
            {
                if (newAVM != null)
                {
                    bool appInChildren = svm.Children.Any(avm =>
                    {
                        var oldAVM = avm as AppViewModel;
                        return oldAVM != null && oldAVM.App.AppId == newAVM.App.AppId;
                    });

                    if (!appInChildren)
                    {
                        svm.Children.Add(newAVM);
                    }
                }
            }
        }

        /// <summary>
        /// Remove all avms from svm.Children which don't appear in currentApps.
        /// </summary>
        /// <param name="svm"></param>
        /// <param name="currentApps"></param>
        private static void RemoveNonexistentApps(SpaceViewModel svm, ObservableCollection<AppViewModel> currentApps)
        {
            var appsToRemove = new ObservableCollection<AppViewModel>();

            foreach (TreeViewItemViewModel priorChild in svm.Children)
            {
                if (priorChild is AppViewModel oldAVM)
                {
                    bool appStillExists = currentApps.Any(avm => avm != null && avm.App.AppId == oldAVM.App.AppId);

                    if (!appStillExists)
                    {
                        appsToRemove.Add(oldAVM);
                    }
                }
            }

            foreach (AppViewModel avm in appsToRemove)
            {
                svm.Children.Remove(avm);
            }
        }

        /// <summary>
        /// Add any svms to ovm.Children which are in currentSpaces but not in ovm.Children.
        /// </summary>
        /// <param name="ovm"></param>
        /// <param name="currentSpaces"></param>
        private static void AddNewSpaces(OrgViewModel ovm, ObservableCollection<SpaceViewModel> currentSpaces)
        {
            foreach (SpaceViewModel newSVM in currentSpaces)
            {
                if (newSVM != null)
                {
                    bool spaceInChildren = ovm.Children.Any(svm =>
                    {
                        var oldSVM = svm as SpaceViewModel;
                        return oldSVM != null && oldSVM.Space.SpaceId == newSVM.Space.SpaceId;
                    });

                    if (!spaceInChildren)
                    {
                        ovm.Children.Add(newSVM);
                    }
                }
            }
        }

        /// <summary>
        /// Remove all svms from ovm.Children which don't appear in currentSpaces.
        /// </summary>
        /// <param name="ovm"></param>
        /// <param name="currentSpaces"></param>
        private static void RemoveNonexistentSpaces(OrgViewModel ovm, ObservableCollection<SpaceViewModel> currentSpaces)
        {
            var spacesToRemove = new ObservableCollection<SpaceViewModel>();

            foreach (TreeViewItemViewModel priorChild in ovm.Children)
            {
                if (priorChild is SpaceViewModel oldSVM)
                {
                    bool spaceStillExists = currentSpaces.Any(svm => svm != null && svm.Space.SpaceId == oldSVM.Space.SpaceId);

                    if (!spaceStillExists)
                    {
                        spacesToRemove.Add(oldSVM);
                    }
                }
            }

            foreach (SpaceViewModel svm in spacesToRemove)
            {
                ovm.Children.Remove(svm);
            }
        }

        /// <summary>
        /// add any ovms to cfivm.Children which are in currentOrgs but not in cfivm.Children.
        /// </summary>
        /// <param name="cfivm"></param>
        /// <param name="currentOrgs"></param>
        private static void AddNewOrgs(CfInstanceViewModel cfivm, ObservableCollection<OrgViewModel> currentOrgs)
        {
            foreach (OrgViewModel newOVM in currentOrgs)
            {
                if (newOVM != null)
                {
                    bool orgInChildren = cfivm.Children.Any(ovm =>
                    {
                        var oldOVM = ovm as OrgViewModel;
                        return oldOVM != null && oldOVM.Org.OrgId == newOVM.Org.OrgId;
                    });

                    if (!orgInChildren)
                    {
                        cfivm.Children.Add(newOVM);
                    }
                }
            }
        }

        /// <summary>
        /// remove all ovms from cfivm.Children which don't appear in currentOrgs.
        /// </summary>
        /// <param name="cfivm"></param>
        /// <param name="currentOrgs"></param>
        private static void RemoveNonexistentOrgs(CfInstanceViewModel cfivm, ObservableCollection<OrgViewModel> currentOrgs)
        {
            var orgsToRemove = new ObservableCollection<OrgViewModel>();

            foreach (TreeViewItemViewModel priorChild in cfivm.Children)
            {
                if (priorChild is OrgViewModel oldOVM)
                {
                    bool orgStillExists = currentOrgs.Any(ovm => ovm != null && ovm.Org.OrgId == oldOVM.Org.OrgId);

                    if (!orgStillExists)
                    {
                        orgsToRemove.Add(oldOVM);
                    }
                }
            }

            foreach (OrgViewModel ovm in orgsToRemove)
            {
                cfivm.Children.Remove(ovm);
            }
        }

        private void UpdateCloudFoundryInstances()
        {
            var loggedInCfs = new ObservableCollection<CloudFoundryInstance>(CloudFoundryService.CloudFoundryInstances.Values);
            var updatedCfInstanceViewModelList = new ObservableCollection<CfInstanceViewModel>();
            foreach (CloudFoundryInstance cf in loggedInCfs)
            {
                updatedCfInstanceViewModelList.Add(new CfInstanceViewModel(cf, _services));
            }

            CloudFoundryList = updatedCfInstanceViewModelList;

            HasCloudTargets = CloudFoundryList.Count > 0;
        }
    }
}
