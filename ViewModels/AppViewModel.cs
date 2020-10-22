using System;
using System.Collections.Generic;
using System.Text;
using TanzuForVS.Models;

namespace TanzuForVS.ViewModels
{
    public class AppViewModel : TreeViewItemViewModel
    {
        readonly CloudFoundryApp _app;

        public AppViewModel(CloudFoundryApp app, IServiceProvider services)
            : base(null, services)
        {
            _app = app;

            this.DisplayText = _app.AppName;
        }
    }
}
