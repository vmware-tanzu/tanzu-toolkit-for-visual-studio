using System;
using Tanzu.Toolkit.Models;

namespace Tanzu.Toolkit.ViewModels
{
    public class AppViewModel : TreeViewItemViewModel
    {
        public AppViewModel(CloudFoundryApp app, IServiceProvider services)
            : base(null, null, services)
        {
            App = app;
            DisplayText = App.AppName;
        }

        public CloudFoundryApp App { get; }

        public bool IsStopped => App.State == "STOPPED";

        public void RefreshAppState()
        {
            RaisePropertyChangedEvent("IsStopped");
        }
    }
}