using System;
using System.Collections.Generic;
using TanzuForVS.Models;

namespace TanzuForVS.ViewModels
{
    public class CloudExplorerViewModel : AbstractViewModel, ICloudExplorerViewModel
    {
        private bool hasCloudTargets;
        private List<CloudFoundryInstanceViewModel> cfs;
        private IServiceProvider _services;


        public CloudExplorerViewModel(IServiceProvider services)
            : base(services)
        {
            _services = services;
            cfs = new List<CloudFoundryInstanceViewModel>();
            HasCloudTargets = CloudFoundryService.CloudFoundryInstances.Keys.Count > 0;
        }

        public List<CloudFoundryInstanceViewModel> CloudFoundryList
        {
            get => this.cfs;

            set
            {
                this.cfs = value;
                this.RaisePropertyChangedEvent("CloudFoundryList");
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

            UpdateCloudFoundryInstances();
        }

        private void UpdateCloudFoundryInstances()
        {
            var loggedInCfs = new List<CloudFoundryInstance>(CloudFoundryService.CloudFoundryInstances.Values);
            var updatedCfInstanceViewModelList = new List<CloudFoundryInstanceViewModel>();
            foreach (CloudFoundryInstance cf in loggedInCfs)
            {
                updatedCfInstanceViewModelList.Add(new CloudFoundryInstanceViewModel(cf, _services));
            }
            CloudFoundryList = updatedCfInstanceViewModelList;

            HasCloudTargets = CloudFoundryList.Count > 0;
        }
    }
}
