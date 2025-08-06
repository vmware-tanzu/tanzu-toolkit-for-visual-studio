using System.Threading.Tasks;
using Tanzu.Toolkit.Models;

namespace Tanzu.Toolkit.ViewModels
{
    public interface ITanzuExplorerViewModel : IViewModel
    {
        bool AuthenticationRequired { get; set; }
        CfInstanceViewModel CloudFoundryConnection { get; }

        bool CanOpenLoginView(object arg);
        void OpenLoginView(object arg);
        bool CanStopCloudFoundryApp(object arg);
        bool CanStartCloudFoundryApp(object arg);
        bool CanOpenDeletionView(object arg);
        bool CanRefreshSpace(object arg);
        bool CanRefreshOrg(object arg);
        bool CanInitiateFullRefresh(object arg);
        bool CanDisplayRecentAppLogs(object arg);
        Task StopCloudFoundryApp(object arg);
        Task StartCloudFoundryApp(object arg);
        Task RefreshSpace(object arg);
        Task RefreshOrg(object arg);
        void RefreshAllItems(object arg);
        Task DisplayRecentAppLogs(object app);
        bool CanReAuthenticate(object arg);
        void ReAuthenticate(object cf);
        void SetConnection(CloudFoundryInstance cf);
        void LogOutCloudFoundry(object arg = null);
        bool CanLogOutCloudFoundry(object arg);
        void OpenDeletionView(object app);
        void StreamAppLogs(object app);
    }
}
