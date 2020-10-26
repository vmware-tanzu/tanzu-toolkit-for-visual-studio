using System;
using TanzuForVS.Models;

namespace TanzuForVS.ViewModels
{
    public class AppViewModel : TreeViewItemViewModel
    {
        public AppViewModel(CloudFoundryApp app, IServiceProvider services)
            : base(null, services)
        {
            App = app;
            this.DisplayText = App.AppName;
        }

        public CloudFoundryApp App { get; }
    }
}
