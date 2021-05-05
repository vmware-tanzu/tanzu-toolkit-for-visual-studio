using System;
using Tanzu.Toolkit.Models;

namespace Tanzu.Toolkit.ViewModels
{
    public class AppViewModel : TreeViewItemViewModel
    {
        public AppViewModel(CloudFoundryApp app, IServiceProvider services)
            : base(null, services)
        {
            App = app;
            DisplayText = App.AppName;
        }

        public CloudFoundryApp App { get; }

        public bool IsStopped
        {
            get
            {
                if (App.State == "STOPPED") return true;
                return false;
            }
        }

        public void SignalIsStoppedChanged()
        {
            RaisePropertyChangedEvent("IsStopped");
        }
    }
}
