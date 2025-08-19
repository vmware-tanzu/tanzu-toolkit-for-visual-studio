using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Tanzu.Toolkit.Models;
using Tanzu.Toolkit.Services.DataPersistence;
using Tanzu.Toolkit.Services.ErrorDialog;
using Tanzu.Toolkit.Services.Threading;
using Tanzu.Toolkit.ViewModels.AppDeletionConfirmation;

namespace Tanzu.Toolkit.ViewModels
{
    public class TanzuExplorerViewModel : AbstractViewModel, ITanzuExplorerViewModel
    {
        internal const string _deleteAppErrorMsg = "Encountered an error while deleting app";
        internal const string _stopAppErrorMsg = "Encountered an error while stopping app";
        internal const string _startAppErrorMsg = "Encountered an error while starting app";
        internal const string _singleLoginErrorTitle = "Unable to add more Tanzu Platform connections.";

        internal const string _singleLoginErrorMessage1 =
            "This version of Tanzu Toolkit for Visual Studio only supports 1 cloud connection at a time; multi-cloud connections will be supported in the future.";

        internal const string _singleLoginErrorMessage2 =
            "If you want to connect to a different cloud, please delete this one by right-clicking on it in the Tanzu Platform Explorer & re-connecting to a new one.";

        internal const string _connectionNameKey = "connection-name";
        internal const string _connectionAddressKey = "connection-api-address";
        internal const string _connectionSslPolicyKey = "connection-should-skip-ssl-cert-validation";
        internal const string _skipCertValidationValue = "skip-ssl-cert-validation";
        internal const string _validateSslCertsValue = "validate-ssl-certificates";

        private CfInstanceViewModel _cloudFoundry;
        private volatile bool _isRefreshingAll = false;
        private readonly object _refreshLock = new object();
        private bool _authenticationRequired;
        private bool _isLoggedIn;
        private ObservableCollection<TreeViewItemViewModel> _treeRoot;
        private readonly IThreadingService _threadingService;
        private readonly IErrorDialog _errorDialogService;
        private readonly IDataPersistenceService _dataPersistenceService;

        public TanzuExplorerViewModel(IServiceProvider services)
            : base(services)
        {
            _errorDialogService = services.GetRequiredService<IErrorDialog>();
            _threadingService = services.GetRequiredService<IThreadingService>();
            _dataPersistenceService = services.GetRequiredService<IDataPersistenceService>();

            var cloudFoundryCredentialsExist = _dataPersistenceService.SavedCloudFoundryCredentialsExist();
            var existingSavedConnectionName = _dataPersistenceService.ReadStringData(_connectionNameKey);
            var existingSavedConnectionAddress = _dataPersistenceService.ReadStringData(_connectionAddressKey);
            var existingSavedConnectionSslPolicy = _dataPersistenceService.ReadStringData(_connectionSslPolicyKey);

            if (!cloudFoundryCredentialsExist || existingSavedConnectionName == null || existingSavedConnectionAddress == null)
            {
                CloudFoundryConnection = null;
            }
            else
            {
                var restoredConnection = new CloudFoundryInstance(
                    name: existingSavedConnectionName,
                    apiAddress: existingSavedConnectionAddress,
                    skipSslCertValidation: existingSavedConnectionSslPolicy != null && existingSavedConnectionSslPolicy == _skipCertValidationValue);

                SetConnection(restoredConnection);
            }
        }

        public CfInstanceViewModel CloudFoundryConnection
        {
            get => _cloudFoundry;

            internal set
            {
                _cloudFoundry = value;

                TreeRoot = value == null
                    ? new ObservableCollection<TreeViewItemViewModel> { new LoginPromptViewModel(Services) }
                    : new ObservableCollection<TreeViewItemViewModel> { value };

                RaisePropertyChangedEvent("CloudFoundryConnection");
            }
        }

        public ObservableCollection<TreeViewItemViewModel> TreeRoot
        {
            get => _treeRoot;

            set
            {
                _treeRoot = value;
                RaisePropertyChangedEvent("TreeRoot");
            }
        }

        /// <summary>
        /// A thread-safe indicator of whether this <see cref="TanzuExplorerViewModel"/> 
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
        /// A flag to indicate whether the view should prompt re-authentication.
        /// </summary>
        public bool AuthenticationRequired
        {
            get => _authenticationRequired;

            set
            {
                _authenticationRequired = value;

                if (value)
                {
                    if (CloudFoundryConnection != null)
                    {
                        CloudFoundryConnection.IsExpanded = false;
                    }
                    else
                    {
                        Logger.Error("Set AuthenticationRequired => true but there is no CloudFoundryConnection to collapse");
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

        public bool CanStopCloudFoundryApp(object arg)
        {
            return true;
        }

        public bool CanStartCloudFoundryApp(object arg)
        {
            return true;
        }

        public bool CanOpenDeletionView(object arg)
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

        public bool CanLogOutCloudFoundry(object arg)
        {
            return IsLoggedIn;
        }

        public async Task OpenLoginViewAsync(object parent)
        {
            if (CloudFoundryConnection != null)
            {
                var errorMsg = _singleLoginErrorMessage1 + Environment.NewLine + _singleLoginErrorMessage2;

                _errorDialogService.DisplayErrorDialog(_singleLoginErrorTitle, errorMsg);
            }
            else
            {
                if (await DialogService.ShowModalAsync(nameof(LoginViewModel)) == null)
                {
                    Logger?.Error("{ClassName}.{MethodName} encountered null DialogResult, indicating that something went wrong trying to construct the view.",
                        nameof(TanzuExplorerViewModel), nameof(OpenLoginViewAsync));
                    var title = "Something went wrong while trying to display login window";
                    var msg = "View construction failed";
                    ErrorService.DisplayErrorDialog(title, msg);
                }
            }
        }

        public async Task OpenDeletionViewAsync(object app)
        {
            if (app is CloudFoundryApp cfApp)
            {
                var appDeletionConfirmationViewModel = Services.GetRequiredService<IAppDeletionConfirmationViewModel>();
                await appDeletionConfirmationViewModel.ShowConfirmationAsync(cfApp);
            }
        }

        public async Task StopCloudFoundryAppAsync(object app)
        {
            if (app is CloudFoundryApp cfApp)
            {
                var stopResult = await CloudFoundryConnection.CfClient.StopAppAsync(cfApp);
                if (!stopResult.Succeeded)
                {
                    Logger.Error(_stopAppErrorMsg + " {AppName}. {StopResult}", cfApp.AppName, stopResult.ToString());
                    _errorDialogService.DisplayErrorDialog($"{_stopAppErrorMsg} {cfApp.AppName}.", stopResult.Explanation);
                }
            }
        }

        public async Task StartCloudFoundryAppAsync(object app)
        {
            if (app is CloudFoundryApp cfApp)
            {
                var startResult = await CloudFoundryConnection.CfClient.StartAppAsync(cfApp);
                if (!startResult.Succeeded)
                {
                    Logger.Error(_startAppErrorMsg + " {AppName}. {StartResult}", cfApp.AppName, startResult.ToString());
                    _errorDialogService.DisplayErrorDialog($"{_startAppErrorMsg} {cfApp.AppName}.", startResult.Explanation);
                }
            }
        }

        public async Task DisplayRecentAppLogsAsync(object app)
        {
            if (app is CloudFoundryApp cfApp)
            {
                var recentLogsResult = await CloudFoundryConnection.CfClient.GetRecentLogsAsync(cfApp);
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
                    var outputView = await ViewLocatorService.GetViewByViewModelNameAsync(nameof(OutputViewModel), $"\"{cfApp.AppName}\" Logs") as IView;
                    var outputViewModel = outputView?.ViewModel as IOutputViewModel;

                    outputView?.DisplayView();

                    outputViewModel?.AppendLine(recentLogsResult.Content);
                }
            }
            else
            {
                Logger.Error(
                    $"{nameof(DisplayRecentAppLogsAsync)} received expected argument 'app' to be of type '{typeof(CloudFoundryApp)}', but instead received type '{app.GetType()}'.");
            }
        }

        public async Task StreamAppLogsAsync(object app)
        {
            if (!(app is CloudFoundryApp cfApp))
            {
                return;
            }

            try
            {
                var viewTitle = $"Logs for {cfApp.AppName}";
                var outputView = await ViewLocatorService.GetViewByViewModelNameAsync(nameof(OutputViewModel), viewTitle) as IView;
                var outputViewModel = outputView?.ViewModel as IOutputViewModel;
                _ = outputViewModel?.BeginStreamingAppLogsForAppAsync(cfApp, outputView);
            }
            catch (Exception ex)
            {
                Logger.Error("Caught exception trying to stream app logs for '{AppName}': {AppLogsException}", cfApp.AppName, ex);
                ErrorService.DisplayErrorDialog("Error displaying app logs", $"Something went wrong while trying to display logs for {cfApp.AppName}, please try again.");
            }
        }

        public async Task RefreshSpaceAsync(object arg)
        {
            if (arg is SpaceViewModel spaceViewModel)
            {
                await spaceViewModel.UpdateAllChildrenAsync();
            }
        }

        public async Task RefreshOrgAsync(object arg)
        {
            if (arg is OrgViewModel orgViewModel)
            {
                await orgViewModel.UpdateAllChildrenAsync();
            }
        }

        public void BackgroundRefreshAllItems(object arg = null)
        {
            if (CloudFoundryConnection == null)
            {
                IsRefreshingAll = false;
                ThreadingService.IsPolling = false;
            }
            else if (!IsRefreshingAll)
            {
                IsRefreshingAll = true;
                _ = _threadingService.StartBackgroundTaskAsync(UpdateAllTreeItems);
            }
        }

        public void SetConnection(CloudFoundryInstance cf)
        {
            if (CloudFoundryConnection != null)
            {
                return;
            }

            CloudFoundryConnection = new CfInstanceViewModel(cf, this, Services);

            AuthenticationRequired = false;

            IsLoggedIn = true;

            if (!ThreadingService.IsPolling)
            {
                ThreadingService.StartRecurrentUITaskInBackground(BackgroundRefreshAllItems, null, 10);
            }

            _dataPersistenceService.WriteStringData(_connectionNameKey, CloudFoundryConnection.CloudFoundryInstance.InstanceName);
            _dataPersistenceService.WriteStringData(_connectionAddressKey, CloudFoundryConnection.CloudFoundryInstance.ApiAddress);
            _dataPersistenceService.WriteStringData(_connectionSslPolicyKey,
                CloudFoundryConnection.CloudFoundryInstance.SkipSslCertValidation ? _skipCertValidationValue : _validateSslCertsValue);
        }

        public void LogOutCloudFoundry(object arg = null)
        {
            IsRefreshingAll = false;
            ThreadingService.IsPolling = false;
            CloudFoundryConnection?.CfClient.LogoutCfUser();
            _dataPersistenceService.ClearData(_connectionNameKey);
            _dataPersistenceService.ClearData(_connectionSslPolicyKey);
            AuthenticationRequired = true;
            IsLoggedIn = false;
            CloudFoundryConnection = null;
        }

        public async Task ReAuthenticateAsync(object arg)
        {
            LogOutCloudFoundry(CloudFoundryConnection);
            await OpenLoginViewAsync(null);
        }

        internal async Task UpdateAllTreeItems()
        {
            await TreeRoot[0].UpdateAllChildrenAsync();
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