using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using Tanzu.Toolkit.VisualStudio.Models;

namespace Tanzu.Toolkit.VisualStudio.ViewModels
{
    public class CloudExplorerViewModel : AbstractViewModel, ICloudExplorerViewModel
    {
        private bool hasCloudTargets;
        private List<CfInstanceViewModel> cfs;
        private IServiceProvider _services;

        internal static readonly string _stopAppErrorMsg = "Encountered an error while stopping app";
        internal static readonly string _startAppErrorMsg = "Encountered an error while starting app";


        public CloudExplorerViewModel(IServiceProvider services)
            : base(services)
        {
            _services = services;
            cfs = new List<CfInstanceViewModel>();
            HasCloudTargets = CloudFoundryService.CloudFoundryInstances.Keys.Count > 0;
        }


        public List<CfInstanceViewModel> CloudFoundryList
        {
            get => cfs;

            set
            {
                cfs = value;
                RaisePropertyChangedEvent("CloudFoundryList");
            }
        }

        public bool HasCloudTargets
        {
            get => hasCloudTargets;

            set
            {
                hasCloudTargets = value;
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


        public void OpenLoginView(object parent)
        {
            var result = DialogService.ShowDialog(typeof(AddCloudDialogViewModel).Name);

            UpdateCloudFoundryInstances();
        }

        public async Task StopCfApp(object app)
        {
            if (app is CloudFoundryApp cfApp)
            {
                var stopResult = await CloudFoundryService.StopAppAsync(cfApp);
                if (!stopResult.Succeeded)
                {
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
                    DialogService.DisplayErrorDialog($"{_startAppErrorMsg} {cfApp.AppName}.", startResult.Explanation);
                }
            }
        }

        public async Task DeleteCfApp(object app)
        {
            var cfApp = app as CloudFoundryApp;
            if (cfApp != null)
            {
                await CloudFoundryService.DeleteAppAsync(cfApp);
                // TODO: display error dialog if something goes wrong with this request
            }
        }

        public async Task RefreshCfInstance(object cfInstanceViewModel)
        {
            if (cfInstanceViewModel is CfInstanceViewModel cfivm)
            {
                var currentOrgs = await cfivm.FetchChildren();

                RemoveNonexistentOrgs(cfivm, currentOrgs);
                AddNewOrgs(cfivm, currentOrgs);
            }
        }

        public async Task RefreshOrg(object orgViewModel)
        {
            if (orgViewModel is OrgViewModel ovm)
            {
                var currentSpaces = await ovm.FetchChildren();

                RemoveNonexistentSpaces(ovm, currentSpaces);
                AddNewSpaces(ovm, currentSpaces);
            }
        }

        public async Task RefreshSpace(object spaceViewModel)
        {
            if (spaceViewModel is SpaceViewModel svm)
            {
                var currentApps = await svm.FetchChildren();

                RemoveNonexistentApps(svm, currentApps);
                AddNewApps(svm, currentApps);
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
            // before refreshing each cf instance, check the Model's record of Cloud 
            // connections & make sure `CloudFoundryList` matches those values 
            SyncCloudFoundryList();

            foreach (CfInstanceViewModel cfivm in CloudFoundryList)
            {
                await RefreshCfInstance(cfivm);

                foreach (TreeViewItemViewModel cfChild in cfivm.Children)
                {
                    if (cfChild is OrgViewModel ovm)
                    {
                        await RefreshOrg(ovm);

                        foreach (TreeViewItemViewModel orgChild in ovm.Children)
                        {
                            if (orgChild is SpaceViewModel svm)
                            {
                                await RefreshSpace(svm);

                                foreach (TreeViewItemViewModel spaceChild in svm.Children)
                                {
                                    if (spaceChild is AppViewModel avm) RefreshApp(avm);
                                }
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Update CloudFoundryList with values from the Model's record of Cloud instances
        /// Do not re-assign CloudFoundryList (avoid raising property changed event)
        /// </summary>
        private void SyncCloudFoundryList()
        {
            var loggedInCfs = new List<CloudFoundryInstance>(CloudFoundryService.CloudFoundryInstances.Values);
            var updatedCfInstanceViewModelList = new List<CfInstanceViewModel>();
            foreach (CloudFoundryInstance cf in loggedInCfs)
            {
                updatedCfInstanceViewModelList.Add(new CfInstanceViewModel(cf, _services));
            }

            RemoveNonexistentCfsFromCloudFoundryList(updatedCfInstanceViewModelList);
            AddNewCfsToCloudFoundryList(updatedCfInstanceViewModelList);

            HasCloudTargets = CloudFoundryList.Count > 0;
        }

        private void AddNewCfsToCloudFoundryList(List<CfInstanceViewModel> updatedCfInstanceViewModelList)
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

                    if (!cfWasAlreadyInCloudFoundryList) CloudFoundryList.Add(newCFIVM);
                }
            }
        }

        private void RemoveNonexistentCfsFromCloudFoundryList(List<CfInstanceViewModel> updatedCfInstanceViewModelList)
        {
            var cfsToRemove = new List<CfInstanceViewModel>();

            foreach (CfInstanceViewModel oldCFIVM in CloudFoundryList)
            {
                // if oldCFIVM isn't in updatedCfInstanceViewModelList, remove it from CloudFoundryList
                if (oldCFIVM != null)
                {
                    bool cfConnectionStillExists = updatedCfInstanceViewModelList.Any(cfivm => cfivm != null
                        && cfivm.CloudFoundryInstance.ApiAddress == oldCFIVM.CloudFoundryInstance.ApiAddress);

                    if (!cfConnectionStillExists) cfsToRemove.Add(oldCFIVM);
                }
            }

            foreach (CfInstanceViewModel cfivm in cfsToRemove) CloudFoundryList.Remove(cfivm);
        }


        /// <summary>
        /// Add any avms to svm.Children which are in currentApps but not in svm.Children
        /// </summary>
        /// <param name="svm"></param>
        /// <param name="currentApps"></param>
        private static void AddNewApps(SpaceViewModel svm, List<AppViewModel> currentApps)
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

                    if (!appInChildren) svm.Children.Add(newAVM);
                }
            }
        }

        /// <summary>
        /// Remove all avms from svm.Children which don't appear in currentApps
        /// </summary>
        /// <param name="svm"></param>
        /// <param name="currentApps"></param>
        private static void RemoveNonexistentApps(SpaceViewModel svm, List<AppViewModel> currentApps)
        {
            var appsToRemove = new List<AppViewModel>();

            foreach (TreeViewItemViewModel priorChild in svm.Children)
            {
                if (priorChild is AppViewModel oldAVM)
                {
                    bool appStillExists = currentApps.Any(avm => avm != null && avm.App.AppId == oldAVM.App.AppId);

                    if (!appStillExists) appsToRemove.Add(oldAVM);
                }
            }

            foreach (AppViewModel avm in appsToRemove) svm.Children.Remove(avm);
        }

        /// <summary>
        /// Add any svms to ovm.Children which are in currentSpaces but not in ovm.Children
        /// </summary>
        /// <param name="ovm"></param>
        /// <param name="currentSpaces"></param>
        private static void AddNewSpaces(OrgViewModel ovm, List<SpaceViewModel> currentSpaces)
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

                    if (!spaceInChildren) ovm.Children.Add(newSVM);
                }
            }
        }

        /// <summary>
        /// Remove all svms from ovm.Children which don't appear in currentSpaces
        /// </summary>
        /// <param name="ovm"></param>
        /// <param name="currentSpaces"></param>
        private static void RemoveNonexistentSpaces(OrgViewModel ovm, List<SpaceViewModel> currentSpaces)
        {
            var spacesToRemove = new List<SpaceViewModel>();

            foreach (TreeViewItemViewModel priorChild in ovm.Children)
            {
                if (priorChild is SpaceViewModel oldSVM)
                {
                    bool spaceStillExists = currentSpaces.Any(svm => svm != null && svm.Space.SpaceId == oldSVM.Space.SpaceId);

                    if (!spaceStillExists) spacesToRemove.Add(oldSVM);
                }
            }

            foreach (SpaceViewModel svm in spacesToRemove) ovm.Children.Remove(svm);
        }

        /// <summary>
        /// add any ovms to cfivm.Children which are in currentOrgs but not in cfivm.Children
        /// </summary>
        /// <param name="cfivm"></param>
        /// <param name="currentOrgs"></param>
        private static void AddNewOrgs(CfInstanceViewModel cfivm, List<OrgViewModel> currentOrgs)
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

                    if (!orgInChildren) cfivm.Children.Add(newOVM);
                }
            }
        }

        /// <summary>
        /// remove all ovms from cfivm.Children which don't appear in currentOrgs
        /// </summary>
        /// <param name="cfivm"></param>
        /// <param name="currentOrgs"></param>
        private static void RemoveNonexistentOrgs(CfInstanceViewModel cfivm, List<OrgViewModel> currentOrgs)
        {
            var orgsToRemove = new List<OrgViewModel>();

            foreach (TreeViewItemViewModel priorChild in cfivm.Children)
            {
                if (priorChild is OrgViewModel oldOVM)
                {
                    bool orgStillExists = currentOrgs.Any(ovm => ovm != null && ovm.Org.OrgId == oldOVM.Org.OrgId);

                    if (!orgStillExists) orgsToRemove.Add(oldOVM);
                }
            }

            foreach (OrgViewModel ovm in orgsToRemove) cfivm.Children.Remove(ovm);
        }

        private void UpdateCloudFoundryInstances()
        {
            var loggedInCfs = new List<CloudFoundryInstance>(CloudFoundryService.CloudFoundryInstances.Values);
            var updatedCfInstanceViewModelList = new List<CfInstanceViewModel>();
            foreach (CloudFoundryInstance cf in loggedInCfs)
            {
                updatedCfInstanceViewModelList.Add(new CfInstanceViewModel(cf, _services));
            }
            CloudFoundryList = updatedCfInstanceViewModelList;

            HasCloudTargets = CloudFoundryList.Count > 0;
        }
    }
}
