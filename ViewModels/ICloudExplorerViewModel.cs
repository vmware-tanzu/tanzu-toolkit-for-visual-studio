using System.Threading.Tasks;

namespace TanzuForVS.ViewModels
{
    public interface ICloudExplorerViewModel : IViewModel
    {
        bool HasCloudTargets { get; set; }

        bool CanOpenLoginView(object arg);

        void OpenLoginView(object arg);

        bool CanStopCfApp(object arg);

        Task StopCfApp(object arg);
    }
}
