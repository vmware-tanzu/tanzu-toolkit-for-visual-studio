using Community.VisualStudio.Toolkit;
using Community.VisualStudio.Toolkit.DependencyInjection;
using Community.VisualStudio.Toolkit.DependencyInjection.Core;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Serilog;
using System;
using System.IO;
using System.Linq;
using Tanzu.Toolkit.Services.CfCli;
using Tanzu.Toolkit.Services.ErrorDialog;
using Tanzu.Toolkit.Services.Logging;
using Tanzu.Toolkit.ViewModels.RemoteDebug;
using Tanzu.Toolkit.VisualStudio.Services;
using Project = EnvDTE.Project;
using Task = System.Threading.Tasks.Task;

namespace Tanzu.Toolkit.VisualStudio.Commands
{
    [Command(PackageGuids.guidTanzuToolkitPackageCmdSetString, PackageIds.RemoteDebugId)]
    internal sealed class RemoteDebugCommand : BaseDICommand
    {
        private static IErrorDialog _errorService;
        private static ICfCliService _cfCliService;
        private static ILogger _logger;

        private readonly object _cfEnvironmentLock = new object();

        public RemoteDebugCommand(DIToolkitPackage package, IErrorDialog errorDialog, ICfCliService cfCliService, ILoggingService loggingService)
            : base(package)
        {
            _errorService = errorDialog;
            _cfCliService = cfCliService;
            _logger = loggingService.Logger;
        }

        protected override async Task ExecuteAsync(OleMenuCmdEventArgs e)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(Package.DisposalToken);

            try
            {
                var dte = await ServiceProvider.GetGlobalServiceAsync(typeof(DTE)) as DTE2;

                if (!(((object[])dte.ActiveSolutionProjects).FirstOrDefault() is Project project))
                {
                    const string msg = "No current project found.";
                    _logger.Error(msg);

                    VsShellUtilities.ShowMessageBox(
                        Package,
                        msg,
                        "RemoteDebugCommand",
                        OLEMSGICON.OLEMSGICON_CRITICAL,
                        OLEMSGBUTTON.OLEMSGBUTTON_OK,
                        OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
                    return;
                }

                // var solutionDirectory = Path.GetDirectoryName(dte.Solution.FullName);
                var projectName = project.Name;
                var projectDirectory = Path.GetDirectoryName(project.FullName) ?? throw new Exception($"Unable to locate project directory from '{project.FullName}'");
                string targetFramework = null;
                try
                {
                    targetFramework = project.Properties.Item("FriendlyTargetFramework").Value.ToString();
                }
                catch (ArgumentException)
                {
                    targetFramework = project.Properties.Item("TargetFrameworkMoniker").Value.ToString();
                }
                finally
                {
                    if (string.IsNullOrEmpty(targetFramework))
                    {
                        VsShellUtilities.ShowMessageBox(
                            Package,
                            "Please open an issue with details for reproducing this message",
                            "Unable to identify target framework",
                            OLEMSGICON.OLEMSGICON_CRITICAL,
                            OLEMSGBUTTON.OLEMSGBUTTON_OK,
                            OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
                    }
                    else if (targetFramework.StartsWith(".NETFramework,Version=v"))
                    {
                        VsShellUtilities.ShowMessageBox(
                            Package,
                            $"Remote debugging is not currently supported for projects targeting '{targetFramework}'.",
                            "Not supported",
                            OLEMSGICON.OLEMSGICON_CRITICAL,
                            OLEMSGBUTTON.OLEMSGBUTTON_OK,
                            OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
                    }
                    else
                    {
                        var launchFilePath = Path.Combine(projectDirectory, RemoteDebugViewModel._launchFileName);
                        var initiateDebugCallback = new Action<string, string>((orgName, spaceName) =>
                        {
                            dte.ExecuteCommand("DebugAdapterHost.Logging /On /OutputWindow");
                            lock (_cfEnvironmentLock)
                            {
                                var targetOrgResult = _cfCliService.TargetOrg(orgName);
                                if (!targetOrgResult.Succeeded)
                                {
                                    _logger.Error("Failed to target org '{OrgName}' before invoking DebugAdapterHost.Launch -- cf may not be able to find intended app.", orgName);
                                }

                                var targetSpaceResult = _cfCliService.TargetSpace(spaceName);
                                if (!targetSpaceResult.Succeeded)
                                {
                                    _logger.Error("Failed to target space '{SpaceName}' before invoking DebugAdapterHost.Launch -- cf may not be able to find intended app.", spaceName);
                                }

                                dte.ExecuteCommand("DebugAdapterHost.Launch", $"/LaunchJson:\"{launchFilePath}\"");
                            }
                        });
                        var serviceProvider = await
                            VS.GetServiceAsync<SToolkitServiceProvider<TanzuToolkitForVisualStudioPackage>, IToolkitServiceProvider<TanzuToolkitForVisualStudioPackage>>();
                        IRemoteDebugViewModel remoteDebugViewModel = new RemoteDebugViewModel(projectName, projectDirectory, targetFramework, launchFilePath, initiateDebugCallback, serviceProvider);
                        Microsoft.VisualStudio.PlatformUI.DialogWindow view = new RemoteDebugView(remoteDebugViewModel, new ThemeService());
                        remoteDebugViewModel.ViewOpener = view.Show;
                        remoteDebugViewModel.ViewCloser = view.Hide;
                        view.ShowModal();
                    }
                }
            }
            catch (Exception ex)
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(Package.DisposalToken);
                _logger?.Error("{ClassName} caught exception in {MethodName}: {RemoteDebugException}", nameof(RemoteDebugCommand), nameof(Execute), ex);
                _errorService.DisplayErrorDialog("Unable to initiate remote debugging", ex.Message);
            }
        }
    }
}