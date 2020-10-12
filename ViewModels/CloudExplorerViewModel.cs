using System;
using System.Collections.Generic;
using TanzuForVS.Services.Models;

namespace TanzuForVS.ViewModels
{
    public class CloudExplorerViewModel : AbstractViewModel, ICloudExplorerViewModel
    {
        public CloudExplorerViewModel(IServiceProvider services) 
            : base(services)
        {
            HasCloudTargets = CloudFoundryService.CloudItems.Keys.Count > 0;
        }

        public bool CanOpenLoginView(object arg)
        {
            return true;
        }

        public void OpenLoginView(object parent)
        {
            var result = DialogService.ShowDialog(typeof(AddCloudDialogViewModel).Name);
            CloudItemsList = new List<CloudItem>(CloudFoundryService.CloudItems.Values);
            HasCloudTargets = CloudItemsList.Count > 0;
        }
    }
}
