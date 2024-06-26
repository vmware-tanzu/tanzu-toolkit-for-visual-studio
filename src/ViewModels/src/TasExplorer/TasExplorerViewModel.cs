﻿using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Tanzu.Toolkit.Models;
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
        internal const string _singleLoginErrorTitle = "Unable to add more TAS connections.";
        internal const string _singleLoginErrorMessage1 = "This version of Tanzu Toolkit for Visual Studio only supports 1 cloud connection at a time; multi-cloud connections will be supported in the future.";
        internal const string _singleLoginErrorMessage2 = "If you want to connect to a different cloud, please delete this one by right-clicking on it in the Tanzu Application Service Explorer & re-connecting to a new one.";
        internal const string _connectionNameKey = "connection-name";
        internal const string _connectionAddressKey = "connection-api-address";
        internal const string _connectionSslPolicyKey = "connection-should-skip-ssl-cert-validation";
        internal const string _skipCertValidationValue = "skip-ssl-cert-validation";
        internal const string _validateSslCertsValue = "validate-ssl-certificates";

        private CfInstanceViewModel _tas;
        private volatile bool _isRefreshingAll = false;
        private readonly object _refreshLock = new object();
        private bool _authenticationRequired;
        private bool _isLoggedIn;
        private ObservableCollection<TreeViewItemViewModel> _treeRoot;
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
            var existingSavedConnectionName = _dataPersistenceService.ReadStringData(_connectionNameKey);
            var existingSavedConnectionAddress = _dataPersistenceService.ReadStringData(_connectionAddressKey);
            var existingSavedConnectionSslPolicy = _dataPersistenceService.ReadStringData(_connectionSslPolicyKey);

            if (!savedConnectionCredsExist || existingSavedConnectionName == null || existingSavedConnectionAddress == null)
            {
                TasConnection = null;
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
                else
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
            get => _treeRoot;

            set
            {
                _treeRoot = value;
                RaisePropertyChangedEvent("TreeRoot");
            }
        }

        /// <summary>
        /// A thread-safe indicator of whether this <see cref="TasExplorerViewModel"/> 
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

        public bool CanLogOutTas(object arg)
        {
            return IsLoggedIn;
        }


        public void OpenLoginView(object parent)
        {
            if (TasConnection != null)
            {
                var errorMsg = _singleLoginErrorMessage1 + Environment.NewLine + _singleLoginErrorMessage2;

                _errorDialogService.DisplayErrorDialog(_singleLoginErrorTitle, errorMsg);
            }
            else
            {
                if (DialogService.ShowModal(nameof(LoginViewModel)) == null)
                {
                    Logger?.Error("{ClassName}.{MethodName} encountered null DialogResult, indicating that something went wrong trying to construct the view.", nameof(TasExplorerViewModel), nameof(OpenLoginView));
                    var title = "Something went wrong while trying to display login window";
                    var msg = "View construction failed" +
                        Environment.NewLine + Environment.NewLine +
                        "If this issue persists, please contact tas-vs-extension@vmware.com";
                    ErrorService.DisplayErrorDialog(title, msg);
                }
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

                    outputView?.DisplayView();

                    outputViewModel?.AppendLine(recentLogsResult.Content);
                }
            }
            else
            {
                Logger.Error($"TasExplorerViewModel.GetRecentAppLogs received expected argument 'app' to be of type '{typeof(CloudFoundryApp)}', but instead received type '{app.GetType()}'.");
            }
        }

        public void StreamAppLogs(object app)
        {
            if (!(app is CloudFoundryApp cfApp))
            {
                return;
            }

            try
            {
                var viewTitle = $"Logs for {cfApp.AppName}";
                var outputView = ViewLocatorService.GetViewByViewModelName(nameof(OutputViewModel), viewTitle) as IView;
                var outputViewModel = outputView?.ViewModel as IOutputViewModel;
                _ = outputViewModel?.BeginStreamingAppLogsForAppAsync(cfApp, outputView);
            }
            catch (Exception ex)
            {
                Logger.Error("Caught exception trying to stream app logs for '{AppName}': {AppLogsException}", cfApp.AppName, ex);
                ErrorService.DisplayErrorDialog("Error displaying app logs", $"Something went wrong while trying to display logs for {cfApp.AppName}, please try again.");
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
            if (TasConnection != null)
            {
                return;
            }

            TasConnection = new CfInstanceViewModel(cf, this, Services);

            AuthenticationRequired = false;

            IsLoggedIn = true;

            if (!ThreadingService.IsPolling)
            {
                ThreadingService.StartRecurrentUiTaskInBackground(RefreshAllItems, null, 10);
            }

            _dataPersistenceService.WriteStringData(_connectionNameKey, TasConnection.CloudFoundryInstance.InstanceName);
            _dataPersistenceService.WriteStringData(_connectionAddressKey, TasConnection.CloudFoundryInstance.ApiAddress);
            _dataPersistenceService.WriteStringData(_connectionSslPolicyKey,
                TasConnection.CloudFoundryInstance.SkipSslCertValidation ? _skipCertValidationValue : _validateSslCertsValue);
        }

        public void LogOutTas(object arg = null)
        {
            IsRefreshingAll = false;
            ThreadingService.IsPolling = false;
            TasConnection.CfClient.LogoutCfUser();
            _dataPersistenceService.ClearData(_connectionNameKey);
            _dataPersistenceService.ClearData(_connectionAddressKey);
            _dataPersistenceService.ClearData(_connectionSslPolicyKey);
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
