using EnvDTE;
using EnvDTE80;
using Microsoft;
using Microsoft.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.IO;
using System.Threading.Tasks;
using TanzuForVS.ViewModels;
using TanzuForVS.WpfViews;
using Task = System.Threading.Tasks.Task;

namespace TanzuForVS.Commands
{
    /// <summary>
    /// Command handler
    /// </summary>
    internal sealed class PushToCloudFoundryCommand
    {
        /// <summary>
        /// Command ID.
        /// </summary>
        public const int CommandId = 256;

        /// <summary>
        /// Command menu group (command set GUID).
        /// </summary>
        public static readonly Guid CommandSet = new Guid("06b33924-97f3-4d33-804c-472ed0d0cc59");

        /// <summary>
        /// VS Package that provides this command, not null.
        /// </summary>
        private readonly AsyncPackage package;

        private IServiceProvider _services;

        /// <summary>
        /// Initializes a new instance of the <see cref="PushToCloudFoundryCommand"/> class.
        /// Adds our command handlers for menu (commands must exist in the command table file)
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        /// <param name="commandService">Command service to add command to, not null.</param>
        private PushToCloudFoundryCommand(AsyncPackage package, OleMenuCommandService commandService, IServiceProvider services)
        {
            this.package = package ?? throw new ArgumentNullException(nameof(package));
            commandService = commandService ?? throw new ArgumentNullException(nameof(commandService));

            _services = services;

            var menuCommandID = new CommandID(CommandSet, CommandId);
            var menuItem = new MenuCommand(Execute, menuCommandID);
            commandService.AddCommand(menuItem);
        }

        /// <summary>
        /// Gets the instance of the command.
        /// </summary>
        public static PushToCloudFoundryCommand Instance
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the service provider from the owner package.
        /// </summary>
        private Microsoft.VisualStudio.Shell.IAsyncServiceProvider ServiceProvider
        {
            get
            {
                return package;
            }
        }

        /// <summary>
        /// Initializes the singleton instance of the command.
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        public static async Task InitializeAsync(AsyncPackage package, IServiceProvider services)
        {
            // Switch to the main thread - the call to AddCommand in PushToCloudFoundryCommand's constructor requires
            // the UI thread.
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);

            OleMenuCommandService commandService = await package.GetServiceAsync(typeof(IMenuCommandService)) as OleMenuCommandService;
            Instance = new PushToCloudFoundryCommand(package, commandService, services);
        }


        /// <summary>
        /// This function is the callback used to execute the command when the menu item is clicked.
        /// See the constructor to see how the menu item is associated with this function using
        /// OleMenuCommandService service and MenuCommand class.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event args.</param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "VSTHRD100:Avoid async void methods", Justification = "Method handles all exceptions.")]
        private async void Execute(object sender, EventArgs e)
        {
            try
            {
                foreach (string projectPath in await GetSelectedProjectPathsAsync())
                {
                    var viewModel = new DeploymentDialogViewModel(_services, projectPath);
                    var view = new DeploymentDialogView(viewModel);

                    var deployWindow = new DeploymentWindow
                    {
                        Content = view
                    };

                    deployWindow.ShowModal();
                }
            }
            catch (Exception ex)
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

                // TODO: decide what to do if we encounter an error at this stage 
                // (log or message box? etc.)
                VsShellUtilities.ShowMessageBox(
                    package,
                    ex.ToString(),
                            "Unable to push to Cloud Foundry",
                            OLEMSGICON.OLEMSGICON_CRITICAL,
                            OLEMSGBUTTON.OLEMSGBUTTON_OK,
                            OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST
                        );
            }
        }

        private async Task<List<string>> GetSelectedProjectPathsAsync()
        {
            // Ensure project file access happens on the main thread
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            var dte = (DTE2)await package.GetServiceAsync(typeof(DTE));
            Assumes.Present(dte);

            var projectPaths = new List<string>();
            var activeProjects = (Array)dte.ActiveSolutionProjects;
            foreach (Project proj in activeProjects)
            {
                string projectDirectory = Path.GetDirectoryName(proj.FullName);
                string outputPath = proj.ConfigurationManager.ActiveConfiguration.Properties.Item("OutputPath").Value.ToString();
                string pathToBinDirectory = Path.Combine(projectDirectory, outputPath);

                projectPaths.Add(pathToBinDirectory);
            }

            return projectPaths;
        }
    }


    class DeploymentWindow : DialogWindow
    {
        internal DeploymentWindow()
        {
            this.HasMaximizeButton = true;
            this.HasMinimizeButton = true;
        }
    }
}