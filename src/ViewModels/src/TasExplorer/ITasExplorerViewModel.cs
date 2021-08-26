﻿using System.Threading.Tasks;

namespace Tanzu.Toolkit.ViewModels
{
    public interface ITasExplorerViewModel : IViewModel
    {
        bool AuthenticationRequired { get; set; }

        bool CanOpenLoginView(object arg);
        void OpenLoginView(object arg);
        bool CanStopCfApp(object arg);
        bool CanStartCfApp(object arg);
        bool CanDeleteCfApp(object arg);
        bool CanRefreshSpace(object arg);
        bool CanRefreshOrg(object arg);
        bool CanInitiateFullRefresh(object arg);
        bool CanRemoveCloudConnecion(object arg);
        bool CanDisplayRecentAppLogs(object arg);
        Task StopCfApp(object arg);
        Task StartCfApp(object arg);
        Task DeleteCfApp(object arg);
        void RefreshSpace(object arg);
        void RefreshOrg(object arg);
        void RefreshAllItems(object arg);
        void DeleteConnection(object arg);
        Task DisplayRecentAppLogs(object app);
        bool CanReAuthenticate(object arg);
        void ReAuthenticate(object cf);
    }
}
