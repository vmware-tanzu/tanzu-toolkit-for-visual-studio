using System.Threading.Tasks;
using Tanzu.Toolkit.Models;

namespace Tanzu.Toolkit.ViewModels
{
    public interface ITanzuExplorerViewModel : IViewModel
    {
        bool AuthenticationRequired { get; set; }

        CfInstanceViewModel CloudFoundryConnection { get; }

        bool CanOpenLoginView(object arg);

        Task OpenLoginViewAsync(object arg);

        bool CanStopCloudFoundryApp(object arg);

        bool CanStartCloudFoundryApp(object arg);

        bool CanOpenDeletionView(object arg);

        bool CanRefreshSpace(object arg);

        bool CanRefreshOrg(object arg);

        bool CanInitiateFullRefresh(object arg);

        bool CanDisplayRecentAppLogs(object arg);

        Task StopCloudFoundryAppAsync(object arg);

        Task StartCloudFoundryAppAsync(object arg);

        Task RefreshSpaceAsync(object arg);

        Task RefreshOrgAsync(object arg);

        void BackgroundRefreshAllItems(object arg);

        Task DisplayRecentAppLogsAsync(object app);

        bool CanReAuthenticate(object arg);

        Task ReAuthenticateAsync(object cf);

        void SetConnection(CloudFoundryInstance cf);

        void LogOutCloudFoundry(object arg = null);

        bool CanLogOutCloudFoundry(object arg);

        Task OpenDeletionViewAsync(object app);

        Task StreamAppLogsAsync(object app);
    }
}