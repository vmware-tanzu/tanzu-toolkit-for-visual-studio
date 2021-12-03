using System.Threading.Tasks;
using Tanzu.Toolkit.Models;

namespace Tanzu.Toolkit.ViewModels
{
    public interface ITasExplorerViewModel : IViewModel
    {
        bool AuthenticationRequired { get; set; }
        CfInstanceViewModel TasConnection { get; }

        bool CanOpenLoginView(object arg);
        void OpenLoginView(object arg);
        bool CanStopCfApp(object arg);
        bool CanStartCfApp(object arg);
        bool CanDeleteCfApp(object arg);
        bool CanRefreshSpace(object arg);
        bool CanRefreshOrg(object arg);
        bool CanInitiateFullRefresh(object arg);
        bool CanDisplayRecentAppLogs(object arg);
        Task StopCfApp(object arg);
        Task StartCfApp(object arg);
        Task DeleteCfApp(object arg);
        Task RefreshSpace(object arg);
        Task RefreshOrg(object arg);
        void RefreshAllItems(object arg);
        Task DisplayRecentAppLogs(object app);
        bool CanReAuthenticate(object arg);
        void ReAuthenticate(object cf);
        void SetConnection(CloudFoundryInstance cf);
        void LogOutTas(object arg);
        bool CanLogOutTas(object arg);
    }
}
