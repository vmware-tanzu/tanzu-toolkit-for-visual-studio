using System.Threading.Tasks;

namespace Tanzu.Toolkit.ViewModels
{
    public interface ICloudExplorerViewModel : IViewModel
    {
        bool HasCloudTargets { get; set; }

        bool CanOpenLoginView(object arg);

        void OpenLoginView(object arg);

        bool CanStopCfApp(object arg);

        Task StopCfApp(object arg);

        bool CanStartCfApp(object arg);

        Task StartCfApp(object arg);

        bool CanDeleteCfApp(object arg);

        Task DeleteCfApp(object arg);

        Task RefreshSpace(object space);

        bool CanRefreshSpace(object arg);
        Task RefreshAllCloudConnections(object arg);
        bool CanRefreshAllCloudConnections(object arg);
        void RemoveCloudConnection(object arg);
        bool CanRemoveCloudConnecion(object arg);
        Task DisplayRecentAppLogs(object app);
        bool CanDisplayRecentAppLogs(object arg);
    }
}
