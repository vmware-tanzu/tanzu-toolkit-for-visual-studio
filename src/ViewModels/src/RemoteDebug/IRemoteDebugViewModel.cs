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

        bool CanResolveMissingApp(object arg = null);
        void Close(object arg = null);
        Task ResolveMissingAppAsync(object arg = null);
        void CreateLaunchFileIfNonexistent();
        Task BeginRemoteDebuggingAsync(string appName);
        Task EstablishAppToDebugAsync(string expectedAppName);
        void OpenLoginView(object arg = null);
    }
}