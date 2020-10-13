using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using TanzuForVS.Services.CloudFoundry;
using TanzuForVS.Services.Dialog;
using TanzuForVS.Services.Locator;
using TanzuForVS.Services.Models;

namespace TanzuForVS.ViewModels
{
    public abstract class AbstractViewModel : IViewModel, INotifyPropertyChanged
    {
        private object activeView;

        public event PropertyChangedEventHandler PropertyChanged;

        public AbstractViewModel(IServiceProvider services)
        {
            Services = services;
            CloudFoundryService = services.GetRequiredService<ICloudFoundryService>();
            DialogService = services.GetRequiredService<IDialogService>();
            ViewLocatorService = services.GetRequiredService<IViewLocatorService>();
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
