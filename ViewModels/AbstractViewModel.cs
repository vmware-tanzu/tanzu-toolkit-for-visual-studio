﻿using Microsoft.Extensions.DependencyInjection;
using System;
using System.ComponentModel;
using TanzuForVS.Services.CloudFoundry;
using TanzuForVS.Services.Dialog;
using TanzuForVS.Services.Locator;

namespace TanzuForVS.ViewModels
{
    public abstract class AbstractViewModel : IViewModel, INotifyPropertyChanged
    {
        private bool isLoggedIn;
        private string instanceName;
        private object activeView;

        public event PropertyChangedEventHandler PropertyChanged;

        public AbstractViewModel(IServiceProvider services)
        {
            Services = services;
            CloudFoundryService = services.GetRequiredService<ICloudFoundryService>();
            DialogService = services.GetRequiredService<IDialogService>();
            ViewLocatorService = services.GetRequiredService<IViewLocatorService>();
            isLoggedIn = CloudFoundryService.IsLoggedIn;
        }

        public IServiceProvider Services { get; }

        public ICloudFoundryService CloudFoundryService { get; }

        public IViewLocatorService ViewLocatorService { get; }

        public IDialogService DialogService { get; }

        public object ActiveView
        {
            get
            {
                return this.activeView;
            }

            set
            {
                this.activeView = value;
                this.RaisePropertyChangedEvent("ActiveView");
            }
        }

        public bool IsLoggedIn
        {
            get => this.isLoggedIn;

            set
            {
                this.isLoggedIn = value;
                this.RaisePropertyChangedEvent("IsLoggedIn");
            }
        }

        public string InstanceName
        {
            get => instanceName;

            set
            {
                this.instanceName = value;
                this.RaisePropertyChangedEvent("InstanceName");
            }
        }

        protected void RaisePropertyChangedEvent(string propertyName)
        {
            var handler = this.PropertyChanged;

            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
}
