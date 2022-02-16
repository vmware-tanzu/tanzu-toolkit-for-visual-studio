﻿using Microsoft.Extensions.DependencyInjection;
using Serilog;
using System;
using System.ComponentModel;
using Tanzu.Toolkit.Services;
using Tanzu.Toolkit.Services.Dialog;
using Tanzu.Toolkit.Services.File;
using Tanzu.Toolkit.Services.Logging;
using Tanzu.Toolkit.Services.Threading;
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
            DialogService = services.GetRequiredService<IDialogService>();
            ViewLocatorService = services.GetRequiredService<IViewLocatorService>();
            ThreadingService = services.GetRequiredService<IThreadingService>();
            UiDispatcherService = services.GetRequiredService<IUiDispatcherService>();
            FileService = services.GetRequiredService<IFileService>();
            SerializationService = services.GetRequiredService<ISerializationService>();
            var logSvc = services.GetRequiredService<ILoggingService>();
            Logger = logSvc.Logger;
        }

        public IServiceProvider Services { get; }

        public IViewLocatorService ViewLocatorService { get; }

        public IThreadingService ThreadingService { get; }

        public IUiDispatcherService UiDispatcherService { get; }

        public IDialogService DialogService { get; }

        public IFileService FileService { get; }

        public ILogger Logger { get; }
        
        public ISerializationService SerializationService { get; }

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
