using EnvDTE;
using EnvDTE80;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Serilog;
using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Tanzu.Toolkit.Models;
using Tanzu.Toolkit.Services.CfCli;
using Tanzu.Toolkit.Services.CloudFoundry;
using Tanzu.Toolkit.Services.Dialog;
using Tanzu.Toolkit.Services.File;
using Tanzu.Toolkit.Services.Logging;
using Tanzu.Toolkit.ViewModels;
using Tanzu.Toolkit.ViewModels.RemoteDebug;
using Tanzu.Toolkit.VisualStudio.Services;
using Tanzu.Toolkit.VisualStudio.Views;
using BuildEvents = EnvDTE.BuildEvents;

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
        private readonly string LaunchFileName = "launch.json";
        private static ILogger _logger;
        private static ICloudFoundryService _cfClient;
        private static ITasExplorerViewModel _tasExplorer;
        private static IDialogService _dialogService;
        private static ICfCliService _cfCliService;
        private static IFileService _fileService;
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

        public static BuildEvents BuildEvents { get; set; }
        public string WaitForProject { get; private set; }
        public string ProjectLaunchFilePath { get; private set; }
        public bool RemoteDebugLaunchOnDone { get; private set; }

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
            _cfClient = services.GetRequiredService<ICloudFoundryService>();
            _tasExplorer = services.GetRequiredService<ITasExplorerViewModel>();
            _dialogService = services.GetRequiredService<IDialogService>();
            _cfCliService = services.GetRequiredService<ICfCliService>();
            _fileService = services.GetRequiredService<IFileService>();
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
                var targetFrameworkMoniker = project.Properties.Item("FriendlyTargetFramework").Value.ToString();

                var remoteDebugViewModel = new RemoteDebugViewModel(projectName, projectDirectory, targetFrameworkMoniker, _services) as IRemoteDebugViewModel;
                var view = new RemoteDebugView(remoteDebugViewModel, new ThemeService());
                view.ShowDialog();

                //// check to see if vsdbg is installed in app container
                //var sshOutput = string.Empty;
                //var sshOutputCallback = new Action<string>((string output) => { sshOutput += output; });
                //var sshResult = await _cfCliService.RunCfCommandAsync($"ssh {matchingApp.AppName} -c \"ls /home/vcap/app | grep vsdbg\"");
                //var debugAgentInstalled = sshResult.Succeeded && sshOutput != null && sshOutput.Contains("vsdbg");
                //if (!debugAgentInstalled)
                //{
                //    var vsdbgVersion = "latest";
                //    var vsdbgLocation = "/home/vcap/app/vsdbg";
                //    var vsdbgInstallationResult = await _cfCliService.RunCfCommandAsync($"ssh {matchingApp.AppName} -c \"curl - sSL https://aka.ms/getvsdbgsh | bash /dev/stdin -v {vsdbgVersion} -l {vsdbgLocation}\"");
                //    if (!vsdbgInstallationResult.Succeeded)
                //    {
                //        var msg = $"Failed to install debugging agent into remote app \"{matchingApp.AppName}\"";
                //        _logger.Error(msg);
                //        VsShellUtilities.ShowMessageBox(
                //            package,
                //            msg,
                //            title,
                //            OLEMSGICON.OLEMSGICON_CRITICAL,
                //            OLEMSGBUTTON.OLEMSGBUTTON_OK,
                //            OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
                //        return;
                //    }
                //}

                //// look for launch.json file
                //var searchPaths = new string[] { solutionDirectory, projectDirectory };
                //ProjectLaunchFilePath = null;
                //foreach (var path in searchPaths)
                //{
                //    var dir = Path.GetDirectoryName(path);
                //    var fullPath = Path.Combine(dir, LaunchFileName);
                //    if (File.Exists(fullPath))
                //    {
                //        ProjectLaunchFilePath = fullPath;
                //        break;
                //    }
                //}

                //if (ProjectLaunchFilePath == null)
                //{
                //    var launchFileConfig = new RemoteDebugLaunchConfig
                //    {
                //        version = "0.2.0",
                //        adapter = "cf",
                //        adapterArgs = "ssh remote-debug -c \"/tmp/lifecycle/shell /home/vcap/app 'bash -c \\\"/home/vcap/app/vsdbg/vsdbg --interpreter=vscode\\\"'\"",
                //        languageMappings = new Languagemappings
                //        {
                //            CSharp = new CSharp
                //            {
                //                languageId = "3F5162F8-07C6-11D3-9053-00C04FA302A1",
                //                extensions = new string[] { "*" },
                //            },
                //        },
                //        exceptionCategoryMappings = new Exceptioncategorymappings
                //        {
                //            CLR = "449EC4CC-30D2-4032-9256-EE18EB41B62B",
                //            MDA = "6ECE07A9-0EDE-45C4-8296-818D8FC401D4",
                //        },
                //        configurations = new Configuration[]
                //        {
                //            new Configuration
                //            {
                //                name = ".NET Core Launch",
                //                type = "coreclr",
                //                processName = projectName,
                //                request = "attach",
                //                justMyCode = false,
                //                cwd = "/home/vcap/app",
                //                logging = new Logging
                //                {
                //                    engineLogging = true,
                //                },
                //            },
                //        }
                //    };
                //    var newLaunchFileContents = JsonSerializer.Serialize(launchFileConfig);
                //    ProjectLaunchFilePath = Path.Combine(projectDirectory, LaunchFileName);
                //    _fileService.WriteTextToFile(ProjectLaunchFilePath, newLaunchFileContents);
                //}

                //dte.ExecuteCommand("DebugAdapterHost.Launch", $"/LaunchJson:\"{ProjectLaunchFilePath}\"");
            }
            catch (Exception ex)
            {
                _logger.Error("Failed to initiate remote debugging: {RemoteDebuggingError}", ex);
            }
        }
    }


    internal class RemoteDebugLaunchConfig
    {
        internal string version { get; set; }
        internal string adapter { get; set; }
        internal string adapterArgs { get; set; }
        internal Languagemappings languageMappings { get; set; }
        internal Exceptioncategorymappings exceptionCategoryMappings { get; set; }
        internal Configuration[] configurations { get; set; }
    }

    internal class Languagemappings
    {
        [JsonPropertyName("C#")]
        internal CSharp CSharp { get; set; }
    }

    internal class CSharp
    {
        internal string languageId { get; set; }
        internal string[] extensions { get; set; }
    }

    internal class Exceptioncategorymappings
    {
        internal string CLR { get; set; }
        internal string MDA { get; set; }
    }

    internal class Configuration
    {
        internal string name { get; set; }
        internal string type { get; set; }
        internal string processName { get; set; }
        internal string request { get; set; }
        internal bool justMyCode { get; set; }
        internal string cwd { get; set; }
        internal Logging logging { get; set; }
    }

    internal class Logging
    {
        internal bool engineLogging { get; set; }
    }
}
