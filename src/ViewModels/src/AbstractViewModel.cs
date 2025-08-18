using Microsoft.Extensions.DependencyInjection;
using Serilog;
using System;
using System.ComponentModel;
using Tanzu.Toolkit.Services;
using Tanzu.Toolkit.Services.CloudFoundry;
using Tanzu.Toolkit.Services.DataPersistence;
using Tanzu.Toolkit.Services.Dialog;
using Tanzu.Toolkit.Services.ErrorDialog;
using Tanzu.Toolkit.Services.File;
using Tanzu.Toolkit.Services.Logging;
using Tanzu.Toolkit.Services.Serialization;
using Tanzu.Toolkit.Services.Threading;
using Tanzu.Toolkit.Services.ViewLocator;

namespace Tanzu.Toolkit.ViewModels
{
    public abstract class AbstractViewModel : IViewModel, INotifyPropertyChanged
    {
        private object _activeView;
        private ITanzuExplorerViewModel _tanzuExplorer;
        private ICloudFoundryService _cfClient;

        public event PropertyChangedEventHandler PropertyChanged;

        protected AbstractViewModel(IServiceProvider services)
        {
            Services = services;

            try
            {
                var logSvc = services.GetRequiredService<ILoggingService>();
                Logger = logSvc.Logger;
                DialogService = services.GetRequiredService<IDialogService>();
                ViewLocatorService = services.GetRequiredService<IViewLocatorService>();
                ThreadingService = services.GetRequiredService<IThreadingService>();
                UIDispatcherService = services.GetRequiredService<IUIDispatcherService>();
                FileService = services.GetRequiredService<IFileService>();
                SerializationService = services.GetRequiredService<ISerializationService>();
                ErrorService = services.GetRequiredService<IErrorDialog>();
                DataPersistenceService = services.GetRequiredService<IDataPersistenceService>();
            }
            catch (Exception ex)
            {
                Logger?.Error("Unable to construct {ClassName} due to an unattainable service: {ServiceException}", nameof(AbstractViewModel), ex);
            }
        }

        public IServiceProvider Services { get; }

        public IViewLocatorService ViewLocatorService { get; }

        public IThreadingService ThreadingService { get; }

        public IUIDispatcherService UIDispatcherService { get; }

        public IDialogService DialogService { get; }

        public IFileService FileService { get; }

        public IErrorDialog ErrorService { get; }

        protected IDataPersistenceService DataPersistenceService { get; }

        public ILogger Logger { get; }

        public ISerializationService SerializationService { get; }

        public ITanzuExplorerViewModel TanzuExplorer => _tanzuExplorer ?? (_tanzuExplorer = Services.GetRequiredService<ITanzuExplorerViewModel>());

        public ICloudFoundryService CfClient
        {
            get
            {
                if (_cfClient == null)
                {
                    if (TanzuExplorer.CloudFoundryConnection == null)
                    {
                        Logger.Information("Detected null Tanzu Platform connection in AbstractViewModel; prompting login");
                        DialogService.ShowModalAsync(nameof(LoginViewModel)).GetAwaiter().GetResult();
                    }

                    _cfClient = TanzuExplorer.CloudFoundryConnection?.CfClient;
                    if (_cfClient == null)
                    {
                        Logger.Information("CF client still null after prompting login");
                    }
                }

                return _cfClient;
            }

            set => _cfClient = value;
        }

        public object ActiveView
        {
            get => _activeView;

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