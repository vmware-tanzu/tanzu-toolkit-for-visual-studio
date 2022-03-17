using System.Collections.Generic;
using System.Threading.Tasks;
using Tanzu.Toolkit.Models;

namespace Tanzu.Toolkit.ViewModels.RemoteDebug
{
    public interface IRemoteDebugViewModel
    {
        List<CloudFoundryApp> AccessibleApps { get; set; }
        string DialogMessage { get; set; }
        string LoadingMessage { get; set; }
        bool CanProceedToDebug(object arg = null);
        bool CheckForLaunchFile();
        bool CheckForRemoteDebugAgent();
        void Close();
        Task ProceedToDebug(object arg = null);
        void CreateLaunchFile();
        Task InitiateRemoteDebuggingAsync();
        void InstallRemoteDebugAgent();
    }
}