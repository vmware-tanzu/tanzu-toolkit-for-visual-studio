using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using Tanzu.Toolkit.Models;
using Tanzu.Toolkit.Services.DataPersistence;
using Tanzu.Toolkit.Services.ErrorDialog;
using Tanzu.Toolkit.Services.Threading;

namespace Tanzu.Toolkit.ViewModels
{
    public class TasExplorerViewModel : AbstractViewModel, ITasExplorerViewModel
    {
        internal const string _stopAppErrorMsg = "Encountered an error while stopping app";
        internal const string _startAppErrorMsg = "Encountered an error while starting app";
        internal const string _deleteAppErrorMsg = "Encountered an error while deleting app";
        internal const string SingleLoginErrorTitle = "Unable to add more TAS connections.";
        internal const string SingleLoginErrorMessage1 = "This version of Tanzu Toolkit for Visual Studio only supports 1 cloud connection at a time; multi-cloud connections will be supported in the future.";
        internal const string SingleLoginErrorMessage2 = "If you want to connect to a different cloud, please delete this one by right-clicking on it in the Tanzu Application Service Explorer & re-connecting to a new one.";
        internal const string ConnectionNameKey = "connection-name";
        internal const string ConnectionAddressKey = "connection-api-address";

        private CfInstanceViewModel _tas;
        private volatile bool _isRefreshingAll = false;
        private volatile int _numRefreshThreads = 0;
        private object _refreshLock = new object();
        private bool _authenticationRequired;
        private bool _isLoggedIn;
        private ObservableCollection<TreeViewItemViewModel> treeRoot;
        private readonly IServiceProvider _services;
        private readonly IThreadingService _threadingService;
        private readonly IErrorDialog _dialogService;
        private readonly IDataPersistenceService _dataPersistenceService;

        public TasExplorerViewModel(IServiceProvider services)
            : base(services)
        {
            _services = services;
            _dialogService = services.GetRequiredService<IErrorDialog>();
            _threadingService = services.GetRequiredService<IThreadingService>();
            _dataPersistenceService = services.GetRequiredService<IDataPersistenceService>();

            string existingSavedConnectionName = _dataPersistenceService.ReadStringData(ConnectionNameKey);
            string existingSavedConnectionAddress = _dataPersistenceService.ReadStringData(ConnectionAddressKey);

            if (existingSavedConnectionName == null || existingSavedConnectionAddress == null)
            {
                TasConnection = null;
            }
            else
            {
                var restoredConnection = new CloudFoundryInstance(name: existingSavedConnectionName, apiAddress: existingSavedConnectionAddress);

                SetConnection(restoredConnection);
            }
        }

        public CfInstanceViewModel TasConnection
        {
            get => _tas;

            internal set
            {
                _tas = value;

                if (value == null)
                {
                    TreeRoot = new ObservableCollection<TreeViewItemViewModel>
                    {
                        new LoginPromptViewModel(Services),
                    };
                }
                else if (value is CfInstanceViewModel)
                {
                    TreeRoot = new ObservableCollection<TreeViewItemViewModel>
                    {
                        value
                    };
                }

                RaisePropertyChangedEvent("TasConnection");
            }
        }

        public ObservableCollection<TreeViewItemViewModel> TreeRoot
        {
            get => treeRoot;

            set
            {
                treeRoot = value;
                RaisePropertyChangedEvent("TreeRoot");
            }
        }

        /// <summary>
        /// A thread-safe indicator of whether or not this <see cref="TasExplorerViewModel"/> 
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

        /// <summary>
        /// A flag to indicate whether or not the view should prompt re-authentication.
        /// </summary>
        public bool AuthenticationRequired
        {
            get => _authenticationRequired;

            set
            {
                _authenticationRequired = value;

                if (value == true)
                {
                    if (TasConnection != null)
                    {
                        TasConnection.IsExpanded = false;
                    }
                    else
                    {
                        Logger.Error("Set AuthenticationRequired => true but there is no TasConnection to collapse");
                    }
                }

                RaisePropertyChangedEvent("AuthenticationRequired");
            }
        }

        public bool IsLoggedIn
        {
            get => _isLoggedIn;

            set
            {
                _isLoggedIn = value;

                RaisePropertyChangedEvent("IsLoggedIn");
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

        public bool CanDisplayRecentAppLogs(object arg)
        {
            return true;
        }

        public bool CanReAuthenticate(object arg)
        {
            return AuthenticationRequired;
        }

        public bool CanLogOutTas(object arg)
        {
            return IsLoggedIn;
        }


        public void OpenLoginView(object parent)
        {
            if (TasConnection != null)
            {
                var errorMsg = SingleLoginErrorMessage1 + Environment.NewLine + SingleLoginErrorMessage2;

                _dialogService.DisplayErrorDialog(SingleLoginErrorTitle, errorMsg);
            }
            else
            {
                DialogService.ShowDialog(typeof(LoginViewModel).Name);

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
                    if (recentLogsResult.FailureType == Toolkit.Services.FailureType.InvalidRefreshToken)
                    {
                        AuthenticationRequired = true;
                    }

                    Logger.Error($"Unable to retrieve recent logs for {cfApp.AppName}. {recentLogsResult.Explanation}. {recentLogsResult.CmdResult}");
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
                Logger.Error($"TasExplorerViewModel.GetRecentAppLogs received expected argument 'app' to be of type '{typeof(CloudFoundryApp)}', but instead received type '{app.GetType()}'.");
            }
        }

        public async Task RefreshSpace(object arg)
        {
            if (arg is SpaceViewModel spaceViewModel)
            {
                await spaceViewModel.RefreshChildren();
            }
        }

        public async Task RefreshOrg(object arg)
        {
            if (arg is OrgViewModel orgViewModel)
            {
                await orgViewModel.RefreshChildren();
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

                object threadLock = new object();

                if (TasConnection != null && TasConnection.IsExpanded && !TasConnection.IsLoading)
                {
                    var refreshCfTask = new Task(async () =>
                    {
                        await TasConnection.RefreshChildren();

                        foreach (TreeViewItemViewModel cfChild in TasConnection.Children)
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

        public void SetConnection(CloudFoundryInstance cf)
        {
            if (TasConnection == null)
            {
                TasConnection = new CfInstanceViewModel(cf, this, Services);

                AuthenticationRequired = false;

                IsLoggedIn = true;

                if (!ThreadingService.IsPolling)
                {
                    ThreadingService.StartUiBackgroundPoller(RefreshAllItems, null, 10);
                }

                string existingSavedConnectionName = _dataPersistenceService.ReadStringData(ConnectionNameKey);

                if (existingSavedConnectionName != cf.InstanceName)
                {
                    _dataPersistenceService.WriteStringData(ConnectionNameKey, TasConnection.CloudFoundryInstance.InstanceName);
                }

                string existingSavedConnectionAddress = _dataPersistenceService.ReadStringData(ConnectionAddressKey);

                if (existingSavedConnectionAddress != cf.ApiAddress)
                {
                    _dataPersistenceService.WriteStringData(ConnectionAddressKey, TasConnection.CloudFoundryInstance.ApiAddress);
                }
            }
        }

        public void LogOutTas(object arg)
        {
            TasConnection = null;
            IsLoggedIn = false;
        }

        public void ReAuthenticate(object arg)
        {
            LogOutTas(TasConnection);
            OpenLoginView(null);
        }
    }
}
