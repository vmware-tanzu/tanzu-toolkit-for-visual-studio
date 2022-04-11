using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Tanzu.Toolkit.Models;
using Tanzu.Toolkit.Services;
using Tanzu.Toolkit.Services.DataPersistence;
using Tanzu.Toolkit.Services.ErrorDialog;
using Tanzu.Toolkit.Services.Threading;
using Tanzu.Toolkit.Services.ViewLocator;
using Tanzu.Toolkit.ViewModels.AppDeletionConfirmation;

namespace Tanzu.Toolkit.ViewModels
{
    public class TasExplorerViewModel : AbstractViewModel, ITasExplorerViewModel
    {
        internal const string _deleteAppErrorMsg = "Encountered an error while deleting app";
        internal const string _stopAppErrorMsg = "Encountered an error while stopping app";
        internal const string _startAppErrorMsg = "Encountered an error while starting app";
        internal const string SingleLoginErrorTitle = "Unable to add more TAS connections.";
        internal const string SingleLoginErrorMessage1 = "This version of Tanzu Toolkit for Visual Studio only supports 1 cloud connection at a time; multi-cloud connections will be supported in the future.";
        internal const string SingleLoginErrorMessage2 = "If you want to connect to a different cloud, please delete this one by right-clicking on it in the Tanzu Application Service Explorer & re-connecting to a new one.";
        internal const string ConnectionNameKey = "connection-name";
        internal const string ConnectionAddressKey = "connection-api-address";
        internal const string ConnectionSslPolicyKey = "connection-should-skip-ssl-cert-validation";
        internal const string SkipCertValidationValue = "skip-ssl-cert-validation";
        internal const string ValidateSslCertsValue = "validate-ssl-certificates";

        private CfInstanceViewModel _tas;
        private volatile bool _isRefreshingAll = false;
        private object _refreshLock = new object();
        private bool _authenticationRequired;
        private bool _isLoggedIn;
        private ObservableCollection<TreeViewItemViewModel> treeRoot;
        private readonly IThreadingService _threadingService;
        private readonly IErrorDialog _errorDialogService;
        private readonly IDataPersistenceService _dataPersistenceService;
        private readonly IViewLocatorService _viewLocatorService;

        public TasExplorerViewModel(IServiceProvider services)
            : base(services)
        {
            _errorDialogService = services.GetRequiredService<IErrorDialog>();
            _threadingService = services.GetRequiredService<IThreadingService>();
            _dataPersistenceService = services.GetRequiredService<IDataPersistenceService>();
            _viewLocatorService = services.GetRequiredService<IViewLocatorService>();

            var savedConnectionCredsExist = _dataPersistenceService.SavedCfCredsExist();
            var existingSavedConnectionName = _dataPersistenceService.ReadStringData(ConnectionNameKey);
            var existingSavedConnectionAddress = _dataPersistenceService.ReadStringData(ConnectionAddressKey);
            var existingSavedConnectionSslPolicy = _dataPersistenceService.ReadStringData(ConnectionSslPolicyKey);

            if (!savedConnectionCredsExist || existingSavedConnectionName == null || existingSavedConnectionAddress == null)
            {
                TasConnection = null;
            }
            else
            {
                var restoredConnection = new CloudFoundryInstance(
                    name: existingSavedConnectionName,
                    apiAddress: existingSavedConnectionAddress,
                    skipSslCertValidation: existingSavedConnectionSslPolicy != null && existingSavedConnectionSslPolicy == SkipCertValidationValue);

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

        public bool CanOpenDeletionView(object arg)
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

                _errorDialogService.DisplayErrorDialog(SingleLoginErrorTitle, errorMsg);
            }
            else
            {
                DialogService.ShowDialog(typeof(LoginViewModel).Name);

            }
        }

        public void OpenDeletionView(object app)
        {
            if (app is CloudFoundryApp cfApp)
            {
                var appDeletionConfirmationViewModel = Services.GetRequiredService<IAppDeletionConfirmationViewModel>();
                appDeletionConfirmationViewModel.ShowConfirmation(cfApp);
            }
        }

        public async Task StopCfApp(object app)
        {
            if (app is CloudFoundryApp cfApp)
            {
                var stopResult = await TasConnection.CfClient.StopAppAsync(cfApp);
                if (!stopResult.Succeeded)
                {
                    Logger.Error(_stopAppErrorMsg + " {AppName}. {StopResult}", cfApp.AppName, stopResult.ToString());
                    _errorDialogService.DisplayErrorDialog($"{_stopAppErrorMsg} {cfApp.AppName}.", stopResult.Explanation);
                }
            }
        }

        public async Task StartCfApp(object app)
        {
            if (app is CloudFoundryApp cfApp)
            {
                var startResult = await TasConnection.CfClient.StartAppAsync(cfApp);
                if (!startResult.Succeeded)
                {
                    Logger.Error(_startAppErrorMsg + " {AppName}. {StartResult}", cfApp.AppName, startResult.ToString());
                    _errorDialogService.DisplayErrorDialog($"{_startAppErrorMsg} {cfApp.AppName}.", startResult.Explanation);
                }
            }
        }

        public async Task DisplayRecentAppLogs(object app)
        {
            if (app is CloudFoundryApp cfApp)
            {
                var recentLogsResult = await TasConnection.CfClient.GetRecentLogsAsync(cfApp);
                if (!recentLogsResult.Succeeded)
                {
                    if (recentLogsResult.FailureType == Toolkit.Services.FailureType.InvalidRefreshToken)
                    {
                        AuthenticationRequired = true;
                    }

                    Logger.Error($"Unable to retrieve recent logs for {cfApp.AppName}. {recentLogsResult.Explanation}. {recentLogsResult.CmdResult}");
                    _errorDialogService.DisplayWarningDialog($"Unable to retrieve recent logs for {cfApp.AppName}.", recentLogsResult.Explanation);
                }
                else
                {
                    var outputView = ViewLocatorService.GetViewByViewModelName(nameof(OutputViewModel), $"\"{cfApp.AppName}\" Logs") as IView;
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

        public void StreamAppLogs(object app)
        {
            if (app is CloudFoundryApp cfApp)
            {
                try
                {
                    var viewTitle = $"Logs for {cfApp.AppName}";
                    var outputView = ViewLocatorService.GetViewByViewModelName(nameof(OutputViewModel), viewTitle) as IView;
                    var outputViewModel = outputView.ViewModel as IOutputViewModel;
                    var _ = outputViewModel.BeginStreamingAppLogsForAppAsync(cfApp, outputView);
                }
                catch (Exception ex)
                {
                    Logger.Error("Caught exception trying to stream app logs for '{AppName}': {AppLogsException}", cfApp.AppName, ex);
                    ErrorService.DisplayErrorDialog("Error displaying app logs", $"Something went wrong while trying to display logs for {cfApp.AppName}, please try again.");
                }
            }
        }

        public async Task RefreshSpace(object arg)
        {
            if (arg is SpaceViewModel spaceViewModel)
            {
                await spaceViewModel.UpdateAllChildren();
            }
        }

        public async Task RefreshOrg(object arg)
        {
            if (arg is OrgViewModel orgViewModel)
            {
                await orgViewModel.UpdateAllChildren();
            }
        }

        public void RefreshAllItems(object arg = null)
        {
            if (TasConnection == null)
            {
                IsRefreshingAll = false;
                ThreadingService.IsPolling = false;
            }
            else if (!IsRefreshingAll)
            {
                IsRefreshingAll = true;
                _threadingService.StartBackgroundTask(UpdateAllTreeItems);
            }
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
                    ThreadingService.StartRecurrentUiTaskInBackground(RefreshAllItems, null, 10);
                }

                _dataPersistenceService.WriteStringData(ConnectionNameKey, TasConnection.CloudFoundryInstance.InstanceName);
                _dataPersistenceService.WriteStringData(ConnectionAddressKey, TasConnection.CloudFoundryInstance.ApiAddress);
                _dataPersistenceService.WriteStringData(ConnectionSslPolicyKey,
                    TasConnection.CloudFoundryInstance.SkipSslCertValidation ? SkipCertValidationValue : ValidateSslCertsValue);
            }
        }

        public void LogOutTas(object arg = null)
        {
            IsRefreshingAll = false;
            ThreadingService.IsPolling = false; 
            TasConnection.CfClient.LogoutCfUser();
            _dataPersistenceService.ClearData(ConnectionNameKey);
            _dataPersistenceService.ClearData(ConnectionAddressKey);
            _dataPersistenceService.ClearData(ConnectionSslPolicyKey);
            IsLoggedIn = false;
            TasConnection = null;
        }

        public void ReAuthenticate(object arg)
        {
            LogOutTas(TasConnection);
            OpenLoginView(null);
        }

        internal async Task UpdateAllTreeItems()
        {
            await TreeRoot[0].UpdateAllChildren();
            if (TreeRoot.Count < 1)
            {
                IsRefreshingAll = false;
            }
            else if (TreeRoot[0] is CfInstanceViewModel cf)
            {
                IsRefreshingAll = cf.IsLoading;
            }
        }
    }
}
