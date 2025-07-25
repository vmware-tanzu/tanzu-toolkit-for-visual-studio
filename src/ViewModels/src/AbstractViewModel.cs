using Microsoft.Extensions.DependencyInjection;
using Serilog;
using System;
using System.ComponentModel;
using Tanzu.Toolkit.Services;
using Tanzu.Toolkit.Services.CloudFoundry;
using Tanzu.Toolkit.Services.Dialog;
using Tanzu.Toolkit.Services.ErrorDialog;
using Tanzu.Toolkit.Services.File;
using Tanzu.Toolkit.Services.Logging;
using Tanzu.Toolkit.Services.Threading;
using Tanzu.Toolkit.Services.ViewLocator;

namespace Tanzu.Toolkit.ViewModels
{
    public abstract class AbstractViewModel : IViewModel, INotifyPropertyChanged
    {
        private object _activeView;
        private ITasExplorerViewModel _tasExplorer;
        private ICloudFoundryService _cfClient;

        public event PropertyChangedEventHandler PropertyChanged;

        public AbstractViewModel(IServiceProvider services)
        {
            Services = services;

            try
            {
                var logSvc = services.GetRequiredService<ILoggingService>();
                Logger = logSvc.Logger;
                DialogService = services.GetRequiredService<IDialogService>();
                ViewLocatorService = services.GetRequiredService<IViewLocatorService>();
                ThreadingService = services.GetRequiredService<IThreadingService>();
                UiDispatcherService = services.GetRequiredService<IUiDispatcherService>();
                FileService = services.GetRequiredService<IFileService>();
                SerializationService = services.GetRequiredService<ISerializationService>();
                ErrorService = services.GetRequiredService<IErrorDialog>();
            }
            catch (Exception ex)
            {
                Logger?.Error("Unable to construct {ClassName} due to an unattainable service: {ServiceException}", nameof(AbstractViewModel), ex);
            }
        }

        public IServiceProvider Services { get; }

        public IViewLocatorService ViewLocatorService { get; }

        public IThreadingService ThreadingService { get; }

        public IUiDispatcherService UiDispatcherService { get; }

        public IDialogService DialogService { get; }

        public IFileService FileService { get; }

        public IErrorDialog ErrorService { get; }

        public ILogger Logger { get; }

        public ISerializationService SerializationService { get; }

        public ITasExplorerViewModel TasExplorer
        {
            get
            {
                if (_tasExplorer == null)
                {
                    _tasExplorer = Services.GetRequiredService<ITasExplorerViewModel>();
                }
                return _tasExplorer;
            }
        }

        public ICloudFoundryService CfClient
        {
            get
            {
                if (_cfClient == null)
                {
                    if (TasExplorer.TasConnection == null)
                    {
                        Logger.Information("Detected null Tanzu Platform connection in AbstractViewModel; prompting login");
                        DialogService.ShowModal(typeof(LoginViewModel).Name);
                    }
                    _cfClient = TasExplorer.TasConnection?.CfClient;
                    if (_cfClient == null)
                    {
                        Logger.Information("CF client still null after prompting login");
                    }
                }
                return _cfClient;
            }

            set
            {
                _cfClient = value;
            }
        }

        public object ActiveView
        {
            get
            {
                return _activeView;
            }

            set
            {
                _activeView = value;
                RaisePropertyChangedEvent("ActiveView");
            }
        }

        protected void RaisePropertyChangedEvent(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
