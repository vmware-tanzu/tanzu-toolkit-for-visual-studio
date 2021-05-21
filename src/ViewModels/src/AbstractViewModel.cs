using System;
using System.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Tanzu.Toolkit.Services;
using Tanzu.Toolkit.Services.CloudFoundry;
using Tanzu.Toolkit.Services.Dialog;
using Tanzu.Toolkit.Services.Logging;
using Tanzu.Toolkit.Services.ViewLocator;

namespace Tanzu.Toolkit.ViewModels
{
    public abstract class AbstractViewModel : IViewModel, INotifyPropertyChanged
    {
        private object _activeView;

        public event PropertyChangedEventHandler PropertyChanged;

        public AbstractViewModel(IServiceProvider services)
        {
            Services = services;
            CloudFoundryService = services.GetRequiredService<ICloudFoundryService>();
            DialogService = services.GetRequiredService<IDialogService>();
            ViewLocatorService = services.GetRequiredService<IViewLocatorService>();
            UiDispatcherService = services.GetRequiredService<IUiDispatcherService>();
            var logSvc = services.GetRequiredService<ILoggingService>();
            Logger = logSvc.Logger;
        }

        public IServiceProvider Services { get; }

        public ICloudFoundryService CloudFoundryService { get; }

        public IViewLocatorService ViewLocatorService { get; }
        
        public IUiDispatcherService UiDispatcherService { get; }

        public IDialogService DialogService { get; }

        public ILogger Logger { get; }

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
            var handler = PropertyChanged;

            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
}
