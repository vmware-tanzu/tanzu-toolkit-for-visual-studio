using EnvDTE;
using EnvDTE80;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Serilog;
using System;
using System.ComponentModel.Design;
using System.IO;
using System.Linq;
using Tanzu.Toolkit.Services.Logging;
using Tanzu.Toolkit.ViewModels.RemoteDebug;
using Tanzu.Toolkit.VisualStudio.Services;
using Task = System.Threading.Tasks.Task;

namespace Tanzu.Toolkit.VisualStudio
{
    internal sealed class RemoteDebugCommand
    {
        /// <summary>
        /// Command ID.
        /// </summary>
        public const int CommandId = PackageIds.RemoteDebugId;

        /// <summary>
        /// Command menu group (command set GUID).
        /// </summary>
        public static readonly Guid CommandSet = new Guid(PackageGuids.guidTanzuToolkitPackageCmdSetString);

        /// <summary>
        /// VS Package that provides this command, not null.
        /// </summary>
        private readonly AsyncPackage package;
        private static ILogger _logger;
        private static IServiceProvider _services;

        /// <summary>
        /// Initializes a new instance of the <see cref="RemoteDebugCommand"/> class.
        /// Adds our command handlers for menu (commands must exist in the command table file)
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        /// <param name="commandService">Command service to add command to, not null.</param>
        private RemoteDebugCommand(AsyncPackage package, OleMenuCommandService commandService)
        {
            this.package = package ?? throw new ArgumentNullException(nameof(package));
            commandService = commandService ?? throw new ArgumentNullException(nameof(commandService));

            var menuCommandID = new CommandID(CommandSet, CommandId);
            var menuItem = new MenuCommand(Execute, menuCommandID);
            commandService.AddCommand(menuItem);
        }

        /// <summary>
        /// Gets the instance of the command.
        /// </summary>
        public static RemoteDebugCommand Instance
        {
            get;
            private set;
        }

        /// <summary>
        /// Initializes the singleton instance of the command.
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        public static async Task InitializeAsync(AsyncPackage package, IServiceProvider services)
        {
            // Switch to the main thread - the call to AddCommand in RemoteDebugCommand's constructor requires
            // the UI thread.
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);

            _services = services;

            OleMenuCommandService commandService = await package.GetServiceAsync((typeof(IMenuCommandService))) as OleMenuCommandService;
            Instance = new RemoteDebugCommand(package, commandService);
            var loggingSvc = services.GetRequiredService<ILoggingService>();
            _logger = loggingSvc.Logger;
        }

        /// <summary>
        /// This function is the callback used to execute the command when the menu item is clicked.
        /// See the constructor to see how the menu item is associated with this function using
        /// OleMenuCommandService service and MenuCommand class.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event arguments.</param>
        private void Execute(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            var _ = package.JoinableTaskFactory.RunAsync(TryRemoteDebugAsync);
        }

        private async Task TryRemoteDebugAsync()
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);

            try
            {
                var title = "RemoteDebugCommand";
                var dte = (DTE2)Package.GetGlobalService(typeof(SDTE));
                var VsUiShell = await package.GetServiceAsync(typeof(IVsUIShell)) as IVsUIShell;

                if (!(((object[])dte.ActiveSolutionProjects).FirstOrDefault() is Project project))
                {
                    var msg = "No current project found.";
                    _logger.Error(msg);

                    VsShellUtilities.ShowMessageBox(
                        package,
                        msg,
                        title,
                        OLEMSGICON.OLEMSGICON_CRITICAL,
                        OLEMSGBUTTON.OLEMSGBUTTON_OK,
                        OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
                    return;
                }

                var projectName = project.Name;
                var projectUniqueName = project.UniqueName;
                var projectDirectory = Path.GetDirectoryName(project.FullName);
                var solutionDirectory = Path.GetDirectoryName(dte.Solution.FullName);
                var tfm = "netstandard2.1"; // fallback value
                try
                {
                    tfm = project.Properties.Item("FriendlyTargetFramework").Value.ToString();
                }
                catch (ArgumentException)
                {
                    tfm = project.Properties.Item("TargetFrameworkMoniker").Value.ToString();
                }
                finally
                {
                    if (tfm == null)
                    {
                        var errorTitle = "Unable to identify target framework";
                        var errorMsg = $"Proceeding with default target framework 'netstandard2.1'.\n" +
                            "If this is not intended and the issue persists, please reach out to tas-vs-extension@vmware.com";
                        VsShellUtilities.ShowMessageBox(
                            package,
                            errorMsg,
                            errorTitle,
                            OLEMSGICON.OLEMSGICON_CRITICAL,
                        OLEMSGBUTTON.OLEMSGBUTTON_OK,
                        OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
                    }
                    else if (tfm.StartsWith(".NETFramework,Version=v"))
                    {
                        var errorTitle = "Not supported";
                        var errorMsg = $"Remote debugging is not currently supported for projects targeting '{tfm}'.\n" +
                            "If this is a feature you'd like to see implemented in the future, please let the team know! tas-vs-extension@vmware.com";
                        VsShellUtilities.ShowMessageBox(
                            package,
                            errorMsg,
                            errorTitle,
                            OLEMSGICON.OLEMSGICON_CRITICAL,
                        OLEMSGBUTTON.OLEMSGBUTTON_OK,
                        OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
                    }
                    else
                    {
                        var launchFilePath = Path.Combine(projectDirectory, RemoteDebugViewModel._launchFileName);
                        var initiateDebugCallback = new Action(() =>
                        {
                            dte.ExecuteCommand("DebugAdapterHost.Logging /On /OutputWindow");
                            dte.ExecuteCommand("DebugAdapterHost.Launch", $"/LaunchJson:\"{launchFilePath}\"");
                        });

                        var remoteDebugViewModel = new RemoteDebugViewModel(projectName, projectDirectory, tfm, launchFilePath, initiateDebugCallback, services: _services) as IRemoteDebugViewModel;
                        var view = new RemoteDebugView(remoteDebugViewModel, new ThemeService());
                        remoteDebugViewModel.ViewOpener = view.Show;
                        remoteDebugViewModel.ViewCloser = view.Hide;
                        view.ShowDialog(); // open & wait
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Error("Failed to initiate remote debugging: {RemoteDebuggingError}", ex);
            }
        }
    }
}
