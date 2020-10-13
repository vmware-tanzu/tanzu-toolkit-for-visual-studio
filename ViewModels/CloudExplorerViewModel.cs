using System;
using System.Collections.Generic;
using TanzuForVS.Services.Models;

namespace TanzuForVS.ViewModels
{
    public class CloudExplorerViewModel : AbstractViewModel, ICloudExplorerViewModel
    {
        private bool hasCloudTargets;
        private List<CloudFoundryInstance> cloudFoundryInstancesList;

        public CloudExplorerViewModel(IServiceProvider services) 
            : base(services)
        {
            HasCloudTargets = CloudFoundryService.CloudFoundryInstances.Keys.Count > 0;
        }

        public List<CloudFoundryInstance> CloudFoundryInstancesList
        {
            get => cloudFoundryInstancesList;

            set
            {
                this.cloudFoundryInstancesList = value;
                this.RaisePropertyChangedEvent("CloudFoundryInstancesList");
            }
        }

        public bool HasCloudTargets
        {
            get => this.hasCloudTargets;

            set
            {
                this.hasCloudTargets = value;
                this.RaisePropertyChangedEvent("HasCloudTargets");
            }
        }

        public bool CanOpenLoginView(object arg)
        {
            return true;
        }

        public void OpenLoginView(object parent)
        {
            var result = DialogService.ShowDialog(typeof(AddCloudDialogViewModel).Name);
            CloudFoundryInstancesList = new List<CloudFoundryInstance>(CloudFoundryService.CloudFoundryInstances.Values);
            HasCloudTargets = CloudFoundryInstancesList.Count > 0;
        }
    }
}
