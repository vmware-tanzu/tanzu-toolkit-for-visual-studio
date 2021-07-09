using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tanzu.Toolkit.Models;
using Tanzu.Toolkit.Services.ErrorDialog;
using Tanzu.Toolkit.Services.Threading;

namespace Tanzu.Toolkit.ViewModels
{
    public class CloudExplorerViewModel : AbstractViewModel, ICloudExplorerViewModel
    {
        internal static readonly string _stopAppErrorMsg = "Encountered an error while stopping app";
        internal static readonly string _startAppErrorMsg = "Encountered an error while starting app";
        internal static readonly string _deleteAppErrorMsg = "Encountered an error while deleting app";
        

        private bool _hasCloudTargets;
        private ObservableCollection<CfInstanceViewModel> _cfs;
        private volatile bool _isRefreshingAll = false;
        private volatile int _numRefreshThreads = 0;
        private object _refreshLock = new object();
        private readonly IServiceProvider _services;
        private readonly IThreadingService _threadingService;
        private readonly IErrorDialog _dialogService;

        public CloudExplorerViewModel(IServiceProvider services)
            : base(services)
        {
            _dialogService = services.GetRequiredService<IErrorDialog>();
            _services = services;
            _threadingService = services.GetRequiredService<IThreadingService>();

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

        /// <summary>
        /// A thread-safe indicator of whether or not this <see cref="CloudExplorerViewModel"/> 
        /// is in the process of updating all <see cref="CfInstanceViewModel"/>s, 
        /// <see cref="OrgViewModel"/>s, <see cref="SpaceViewModel"/>s & <see cref="AppViewModel"/>s.
        /// </summary>
        public bool IsRefreshingAll
        {
            get
            {
                lock (_refreshLock)
                {
                    return _isRefreshingAll;
                }
            }

            internal set
            {

                lock (_refreshLock)
                {
                    _isRefreshingAll = value;
                }

                RaisePropertyChangedEvent("IsRefreshingAll");
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

        public bool CanInitiateFullRefresh(object arg)
        {
            return !IsRefreshingAll;
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

                _dialogService.DisplayErrorDialog(errorTitle, errorMsg);
            }
            else
            {
                DialogService.ShowDialog(typeof(AddCloudDialogViewModel).Name);

                UpdateCloudFoundryInstances();

                if (HasCloudTargets && !ThreadingService.IsPolling)
                {
                    ThreadingService.StartUiBackgroundPoller(RefreshAllItems, null, 10);
                }
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
                    _dialogService.DisplayErrorDialog($"{_stopAppErrorMsg} {cfApp.AppName}.", stopResult.Explanation);
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
                    _dialogService.DisplayErrorDialog($"{_startAppErrorMsg} {cfApp.AppName}.", startResult.Explanation);
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
                    _dialogService.DisplayErrorDialog($"{_deleteAppErrorMsg} {cfApp.AppName}.", deleteResult.Explanation);
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
                    _dialogService.DisplayErrorDialog($"Unable to retrieve recent logs for {cfApp.AppName}.", recentLogsResult.Explanation);
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

        public void RefreshSpace(object arg)
        {
            if (arg is SpaceViewModel spaceViewModel)
            {
                Task.Run(() => spaceViewModel.RefreshChildren());
            }
        }

        public void RefreshAllItems(object arg)
        {
            if (!IsRefreshingAll)
            {
                _threadingService.StartTask(InitiateFullRefresh);
            }
        }

        internal async Task InitiateFullRefresh()
        {
            await Task.Run(() =>
            {
                IsRefreshingAll = true;

                // before refreshing each cf instance, check the Model's record of Cloud
                // connections & make sure `CloudFoundryList` matches those values
                SyncCloudFoundryList();

                object threadLock = new object();

                foreach (CfInstanceViewModel cfivm in CloudFoundryList)
                {
                    if (cfivm.IsExpanded && !cfivm.IsLoading)
                    {
                        var refreshCfTask = new Task(async () =>
                        {
                            await cfivm.RefreshChildren();

                            foreach (TreeViewItemViewModel cfChild in cfivm.Children)
                            {
                                if (cfChild is OrgViewModel ovm && cfChild.IsExpanded && !cfChild.IsLoading)
                                {
                                    var refreshOrgTask = new Task(async () =>
                                    {
                                        await ovm.RefreshChildren();

                                        foreach (TreeViewItemViewModel orgChild in ovm.Children)
                                        {
                                            if (orgChild is SpaceViewModel svm && orgChild.IsExpanded && !orgChild.IsLoading)
                                            {
                                                var refreshSpaceTask = new Task(async () =>
                                                {
                                                    await svm.RefreshChildren();

                                                    lock (threadLock)
                                                    {
                                                        if (_numRefreshThreads < 1) throw new ArgumentOutOfRangeException();
                                                        _numRefreshThreads -= 1;
                                                    }
                                                });

                                                lock (threadLock)
                                                {
                                                    _numRefreshThreads += 1;
                                                }

                                                _threadingService.StartTask(() => Task.Run(() => refreshSpaceTask.Start())); // wrapped in extra task runner for ease of unit testing
                                            }
                                        }

                                        lock (threadLock)
                                        {
                                            if (_numRefreshThreads < 1) throw new ArgumentOutOfRangeException();
                                            _numRefreshThreads -= 1;
                                        }
                                    });

                                    lock (threadLock)
                                    {
                                        _numRefreshThreads += 1;
                                    }

                                    _threadingService.StartTask(() => Task.Run(() => refreshOrgTask.Start())); // wrapped in extra task runner for ease of unit testing
                                }
                            }

                            lock (threadLock)
                            {
                                if (_numRefreshThreads < 1) throw new ArgumentOutOfRangeException();
                                _numRefreshThreads -= 1;
                            }
                        });

                        lock (threadLock)
                        {
                            _numRefreshThreads += 1;
                        }

                        _threadingService.StartTask(() => Task.Run(() => refreshCfTask.Start())); // wrapped in extra task runner for ease of unit testing
                    }
                }

                int threadsStillRunning = 1;
                int waitTime = 100;
                while (threadsStillRunning > 0)
                {
                    lock (threadLock)
                    {
                        threadsStillRunning = _numRefreshThreads;
                    }

                    Thread.Sleep(waitTime);
                }

                IsRefreshingAll = false;
            });
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
                        UiDispatcherService.RunOnUiThread(() => CloudFoundryList.Add(newCFIVM));
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
                UiDispatcherService.RunOnUiThread(() => CloudFoundryList.Remove(cfivm));
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
