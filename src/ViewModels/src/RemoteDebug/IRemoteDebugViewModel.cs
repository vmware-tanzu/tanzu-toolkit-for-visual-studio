using System;
using System.Collections.Generic;
using System.Threading;
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
        Action<object> CancelDebugging { get; set; }

        bool CanStartDebuggingApp(object arg = null);

        Task StartDebuggingAppAsync(object arg = null);

        void CreateLaunchFileIfNonexistent(string stack, CancellationToken ct);

        Task PromptAppSelectionAsync(string appName);

        Task OpenLoginViewAsync(object arg = null);

        Task DisplayDeploymentWindowAsync(object arg = null);

        bool CanDisplayDeploymentWindow(object arg = null);

        bool CanCancelDebugging(object arg = null);
    }
}