using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using TanzuForVS.Models;

namespace TanzuForVS.ViewModels
{
    public class CloudExplorerViewModel : AbstractViewModel, ICloudExplorerViewModel
    {
        private bool hasCloudTargets;
        private List<CfInstanceViewModel> cfs;
        private IServiceProvider _services;


        public CloudExplorerViewModel(IServiceProvider services)
            : base(services)
        {
            _services = services;
            cfs = new List<CfInstanceViewModel>();
            HasCloudTargets = CloudFoundryService.CloudFoundryInstances.Keys.Count > 0;
        }


        public List<CfInstanceViewModel> CloudFoundryList
        {
            get => this.cfs;

            set
            {
                this.cfs = value;
                this.RaisePropertyChangedEvent("CloudFoundryList");
            }
        }

        public bool HasCloudTargets
        {
            get => this.hasCloudTargets;

            set
            {
                this.hasCloudTargets = value;
                this.RaisePropertyChangedEvent("HasCloudTargets");
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
            var cfApp = app as CloudFoundryApp;
            if (cfApp != null)
            {
                await CloudFoundryService.StopAppAsync(cfApp);
            }
        }

        public async Task StartCfApp(object app)
        {
            var cfApp = app as CloudFoundryApp;
            if (cfApp != null)
            {
                await CloudFoundryService.StartAppAsync(cfApp);
            }
        }

        public async Task DeleteCfApp(object app)
        {
            var cfApp = app as CloudFoundryApp;
            if (cfApp != null)
            {
                await CloudFoundryService.DeleteAppAsync(cfApp);
            }
        }

        public async Task RefreshCfInstance(object cfInstanceViewModel)
        {
            var cfivm = cfInstanceViewModel as CfInstanceViewModel;
            if (cfivm != null)
            {
                var currentOrgs = await cfivm.FetchChildren();

                RemoveNonexistentOrgs(cfivm, currentOrgs);
                AddNewOrgs(cfivm, currentOrgs);
            }
        }

        public async Task RefreshOrg(object orgViewModel)
        {
            var ovm = orgViewModel as OrgViewModel;
            if (ovm != null)
            {
                var currentSpaces = await ovm.FetchChildren();

                RemoveNonexistentSpaces(ovm, currentSpaces);
                AddNewSpaces(ovm, currentSpaces);
            }
        }

        public async Task RefreshSpace(object spaceViewModel)
        {
            var svm = spaceViewModel as SpaceViewModel;
            if (svm != null)
            {
                var currentApps = await svm.FetchChildren();

                RemoveNonexistentApps(svm, currentApps);
                AddNewApps(svm, currentApps);
            }
        }

        public void RefreshApp(object appViewModel)
        {
            var avm = appViewModel as AppViewModel;
            if (avm != null)
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

                foreach (OrgViewModel ovm in cfivm.Children)
                {
                    if (ovm != null)
                    {
                        await RefreshOrg(ovm);

                        foreach (SpaceViewModel svm in ovm.Children)
                        {
                            if (svm != null)
                            {
                                await RefreshSpace(svm);

                                foreach (AppViewModel avm in svm.Children)
                                {
                                    if (avm != null) RefreshApp(avm);
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

            foreach (AppViewModel oldAVM in svm.Children)
            {
                if (oldAVM != null)
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

            foreach (SpaceViewModel oldSVM in ovm.Children)
            {
                if (oldSVM != null)
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

            foreach (OrgViewModel oldOVM in cfivm.Children)
            {
                if (oldOVM != null)
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
