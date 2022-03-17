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
        bool CheckForLaunchFile();
        void Close();
        void ResolveMissingApp(object arg = null);
        void CreateLaunchFile();
        Task EstablishAppToDebugAsync();
        Task EnsureDebuggingAgentInstalledOnRemoteAsync();
    }
}