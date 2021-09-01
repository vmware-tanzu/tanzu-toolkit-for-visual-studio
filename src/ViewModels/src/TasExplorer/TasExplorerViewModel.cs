using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using Tanzu.Toolkit.Models;
using Tanzu.Toolkit.Services.ErrorDialog;
using Tanzu.Toolkit.Services.Threading;

namespace Tanzu.Toolkit.ViewModels
{
    public class TasExplorerViewModel : AbstractViewModel, ITasExplorerViewModel
    {
        internal static readonly string _stopAppErrorMsg = "Encountered an error while stopping app";
        internal static readonly string _startAppErrorMsg = "Encountered an error while starting app";
        internal static readonly string _deleteAppErrorMsg = "Encountered an error while deleting app";

        private CfInstanceViewModel _tas;
        private volatile bool _isRefreshingAll = false;
        private volatile int _numRefreshThreads = 0;
        private object _refreshLock = new object();
        private bool _authenticationRequired;
        private ObservableCollection<TreeViewItemViewModel> treeRoot;
        private readonly IServiceProvider _services;
        private readonly IThreadingService _threadingService;
        private readonly IErrorDialog _dialogService;

        public TasExplorerViewModel(IServiceProvider services)
            : base(services)
        {
            _dialogService = services.GetRequiredService<IErrorDialog>();
            _services = services;
            _threadingService = services.GetRequiredService<IThreadingService>();

            if (CloudFoundryService.ConnectedCf != null)
            {
                TasConnection = new CfInstanceViewModel(CloudFoundryService.ConnectedCf, this, Services);
            }
            else
            {
                TasConnection = null;
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

        public bool CanOpenLoginView(object arg)
        {
            return CloudFoundryService.ConnectedCf == null;
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

        public bool CanReAuthenticate(object arg)
        {
            return AuthenticationRequired;
        }

        public void OpenLoginView(object parent)
        {
            if (CloudFoundryService.ConnectedCf != null)
            {
                var errorTitle = "Unable to add more TAS connections.";
                var errorMsg = "This version of Tanzu Toolkit for Visual Studio only supports 1 cloud connection at a time; multi-cloud connections will be supported in the future.";
                errorMsg += System.Environment.NewLine + "If you want to connect to a different cloud, please delete this one by right-clicking on it in the Tanzu Application Service Explorer & re-connecting to a new one.";

                _dialogService.DisplayErrorDialog(errorTitle, errorMsg);
            }
            else
            {
                DialogService.ShowDialog(typeof(LoginViewModel).Name);

                bool successfullyLoggedIn = CloudFoundryService.ConnectedCf != null;

                if (successfullyLoggedIn)
                {
                    TasConnection = new CfInstanceViewModel(CloudFoundryService.ConnectedCf, this, Services);
                    AuthenticationRequired = false;

                    if (TasConnection != null && !ThreadingService.IsPolling)
                    {
                        ThreadingService.StartUiBackgroundPoller(RefreshAllItems, null, 10);
                    }
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
                    if (recentLogsResult.FailureType == Toolkit.Services.FailureType.InvalidRefreshToken)
                    {
                        AuthenticationRequired = true;
                    }

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

        public void SetConnetion(CloudFoundryInstance cf)
        {
            if (TasConnection == null)
            {
                TasConnection = new CfInstanceViewModel(cf, this, Services);
            }
        }

        public void DeleteConnection(object arg)
        {
            if (arg is CfInstanceViewModel)
            {
                TasConnection = null;
                CloudFoundryService.ConnectedCf = null;
            }
        }

        public void ReAuthenticate(object arg)
        {
            DeleteConnection(TasConnection);
            OpenLoginView(null);
        }
    }
}
