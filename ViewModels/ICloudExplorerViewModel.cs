using System.Collections.Generic;
using TanzuForVS.Models;

namespace TanzuForVS.ViewModels
{
    public interface ICloudExplorerViewModel : IViewModel
    {
        bool HasCloudTargets { get; set; }

        //List<CloudFoundryInstance> CloudFoundryInstancesList { get; set; }
        //CloudFoundryInstance[] CloudFoundryInstances { get; set; }

        bool CanOpenLoginView(object arg);

        void OpenLoginView(object arg);
    }
}
