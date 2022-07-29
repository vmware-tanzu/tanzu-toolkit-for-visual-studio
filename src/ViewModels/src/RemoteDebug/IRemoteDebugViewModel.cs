using System;
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
        Action ViewOpener { get; set; }
        Action ViewCloser { get; set; }

        bool CanStartDebuggingApp(object arg = null);
        void Close(object arg = null);
        Task StartDebuggingAppAsync(object arg = null);
        void CreateLaunchFileIfNonexistent(string stack);
        Task PromptAppSelectionAsync(string appName);
        void OpenLoginView(object arg = null);
        void DisplayDeploymentWindow(object arg = null);
        bool CanDisplayDeploymentWindow(object arg = null);
    }
}
