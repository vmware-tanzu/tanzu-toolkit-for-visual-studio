using Microsoft.Extensions.DependencyInjection;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Tanzu.Toolkit.Models;
using Tanzu.Toolkit.Services.CfCli;
using Tanzu.Toolkit.Services.CloudFoundry;
using Tanzu.Toolkit.Services.Dialog;
using Tanzu.Toolkit.Services.ErrorDialog;
using Tanzu.Toolkit.Services.File;
using Tanzu.Toolkit.Services.Logging;

namespace Tanzu.Toolkit.ViewModels.RemoteDebug
{
    public class RemoteDebugViewModel : AbstractViewModel, IRemoteDebugViewModel
    {
        private readonly ITasExplorerViewModel _tasExplorer;
        private ICloudFoundryService _cfClient;
        private readonly ICfCliService _cfCliService;
        private readonly string _expectedAppName;
        private List<CloudFoundryApp> _accessibleApps;
        private string _dialogMessage;
        private bool _showAppList;
        private string _loadingMessage;
        private CloudFoundryApp _appToDebug;
        private bool _debugExistingApp;
        private bool _pushNewAppToDebug;
        private string _option1Text;
        private string _option2Text;

        public RemoteDebugViewModel(string expectedAppName, IServiceProvider services) : base(services)
        {
            _expectedAppName = expectedAppName;
            _tasExplorer = services.GetRequiredService<ITasExplorerViewModel>();
            _cfCliService = services.GetRequiredService<ICfCliService>();
            Option1Text = $"Push new version of \"{_expectedAppName}\" to debug";
            Option2Text = $"Select existing app to debug";
        }

        public List<CloudFoundryApp> AccessibleApps
        {
            get => _accessibleApps;

            set
            {
                _accessibleApps = value;
                RaisePropertyChangedEvent("AccessibleApps");
            }
        }

        public CloudFoundryApp AppToDebug
        {
            get => _appToDebug;
            set
            {
                _appToDebug = value;
                RaisePropertyChangedEvent("AppToDebug");
            }
        }

        public bool DebugExistingApp
        {
            get => _debugExistingApp;
            set
            {
                if (value)
                {
                    _debugExistingApp = true;
                    PushNewAppToDebug = false;
                }
                else if (PushNewAppToDebug)
                {
                    _debugExistingApp = false;
                }
                RaisePropertyChangedEvent("DebugExistingApp");
            }
        }

        public bool PushNewAppToDebug
        {
            get => _pushNewAppToDebug;
            set
            {
                if (value)
                {
                    _pushNewAppToDebug = true;
                    DebugExistingApp = false;
                }
                else if (DebugExistingApp)
                {
                    _pushNewAppToDebug = false;
                }
                RaisePropertyChangedEvent("PushNewAppToDebug");
            }
        }

        public string DialogMessage
        {
            get => _dialogMessage;

            set
            {
                _dialogMessage = value;
                RaisePropertyChangedEvent("DialogMessage");
            }
        }

        public string Option1Text
        {
            get => _option1Text;
            set
            {
                _option1Text = value;
                RaisePropertyChangedEvent("Option1Text");
            }
        }

        public string Option2Text
        {
            get => _option2Text;
            set
            {
                _option2Text = value;
                RaisePropertyChangedEvent("Option2Text");
            }
        }

        public bool ShowAppList
        {
            get => _showAppList;

            set
            {
                _showAppList = value;
                RaisePropertyChangedEvent("ShowAppList");
            }
        }

        public string LoadingMessage
        {
            get => _loadingMessage;

            set
            {
                _loadingMessage = value;
                RaisePropertyChangedEvent("LoadingMessage");
            }
        }

        public async Task InitiateRemoteDebuggingAsync()
        {
            AppToDebug = null;
            EnsureIsLoggedIn();
            _cfClient = _tasExplorer.TasConnection.CfClient;

            LoadingMessage = "Identifying remote app to debug...";
            var appsResult = await _cfClient.ListAllAppsAsync();
            LoadingMessage = null;

            if (appsResult.Succeeded)
            {
                AccessibleApps = appsResult.Content;
            }
            else
            {
                var title = "Unable to initiate remote debugging";
                var msg = $"Something went wrong while querying apps on {_tasExplorer.TasConnection.CloudFoundryInstance.InstanceName}.\nIt may help to try disconnecting & signing into TAS again; if this issue persists, please contact tas-vs-extension@vmware.com";
                Logger.Error(title + "; " + msg);
                ErrorService.DisplayErrorDialog(title, msg);
                Close();
                return;
            }
            AppToDebug = AccessibleApps.FirstOrDefault(app => app.AppName == _expectedAppName);
            if (AppToDebug == null)
            {
                PromptAppSelection();
            }
        }

        private void EnsureIsLoggedIn()
        {
            var loggedIn = _tasExplorer.TasConnection != null;
            if (!loggedIn)
            {
                DialogService.ShowDialog(typeof(LoginViewModel).Name);
            }
            if (_tasExplorer.TasConnection == null)
            {
                ErrorService.DisplayErrorDialog(string.Empty, "Must be logged in to remotely debug apps on Tanzu Application Service.");
                Close();
            }
        }

        public void PromptAppSelection()
        {
            DialogMessage = $"No app found with a name matching \"{_expectedAppName}\"";
            ShowAppList = true;
        }

        public void ProceedToDebug(object arg = null)
        {
            if (arg is CloudFoundryApp selectedApp)
            {
                AppToDebug = selectedApp;
            }
            else if (arg == null)
            {

            }
        }

        public bool CanProceedToDebug(object arg = null)
        {
            return PushNewAppToDebug || (DebugExistingApp && AppToDebug != null);
        }

        public void PushNewAppWithDebugConfiguration()
        {
            throw new NotImplementedException();
        }

        public bool CheckForRemoteDebugAgent()
        {
            throw new NotImplementedException();
        }

        public void InstallRemoteDebugAgent()
        {
            throw new NotImplementedException();
        }

        public bool CheckForLaunchFile()
        {
            throw new NotImplementedException();
        }

        public void CreateLaunchFile()
        {
            throw new NotImplementedException();
        }

        public void InitiateRemoteDebugging()
        {
            throw new NotImplementedException();
        }

        public void Close()
        {
            DialogService.CloseDialogByName(nameof(LoginViewModel));
        }
    }
}
