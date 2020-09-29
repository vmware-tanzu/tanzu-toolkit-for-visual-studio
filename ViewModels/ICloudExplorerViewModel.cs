namespace TanzuForVS.ViewModels
{
    public interface ICloudExplorerViewModel : IViewModel
    {
        bool CanOpenLoginView(object arg);

        void OpenLoginView(object arg);
    }
}
