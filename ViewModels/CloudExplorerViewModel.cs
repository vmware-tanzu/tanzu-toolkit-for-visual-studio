using System;

namespace TanzuForVS.ViewModels
{
    public class CloudExplorerViewModel : AbstractViewModel, ICloudExplorerViewModel
    {
        public CloudExplorerViewModel(IServiceProvider services) 
            : base(services)
        {
        }

        public bool CanOpenLoginView(object arg)
        {
            return true;
        }

        public void OpenLoginView(object parent)
        {
            var result = DialogService.ShowDialog(typeof(LoginDialogViewModel).Name);
            IsLoggedIn = CloudFoundryService.IsLoggedIn;
            InstanceName = CloudFoundryService.InstanceName;
        }
    }
}
