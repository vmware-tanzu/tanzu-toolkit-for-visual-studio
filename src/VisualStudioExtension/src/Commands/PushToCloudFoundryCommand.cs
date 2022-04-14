using EnvDTE;
using EnvDTE80;
using Microsoft;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.ComponentModel.Design;
using System.IO;
using Tanzu.Toolkit.Services.ErrorDialog;
using Tanzu.Toolkit.ViewModels;
using Tanzu.Toolkit.VisualStudio.Services;
using Tanzu.Toolkit.VisualStudio.Views;
using Tanzu.Toolkit.VisualStudio.VSToolWindows;
using Task = System.Threading.Tasks.Task;

namespace Tanzu.Toolkit.VisualStudio.Commands
{
    /// <summary>
    /// Command handler.
    /// </summary>
    internal sealed class PushToCloudFoundryCommand
    {
        /// <summary>
        /// Command ID.
        /// </summary>
        public const int _commandId = 257;

        /// <summary>
        /// Command menu group (command set GUID).
        /// </summary>
        public static readonly Guid _commandSet = new Guid("f91c88fb-6e17-42a6-878d-f4d16ead7625");

        /// <summary>
        /// VS Package that provides this command, not null.
        /// </summary>
        private readonly AsyncPackage _package;

        private readonly IServiceProvider _services;
        private readonly IErrorDialog _dialogService;

        /// <summary>
        /// Initializes a new instance of the <see cref="PushToCloudFoundryCommand"/> class.
        /// Adds our command handlers for menu (commands must exist in the command table file).
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        /// <param name="commandService">Command service to add command to, not null.</param>
        private PushToCloudFoundryCommand(AsyncPackage package, OleMenuCommandService commandService, IServiceProvider services)
        {
            _package = package ?? throw new ArgumentNullException(nameof(package));
            commandService = commandService ?? throw new ArgumentNullException(nameof(commandService));
            _services = services;
            _dialogService = services.GetRequiredService<IErrorDialog>();
            var menuCommandID = new CommandID(_commandSet, _commandId);
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
        /// Initializes the singleton instance of the command.
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        /// <param name="services"></param>
        public static async Task InitializeAsync(AsyncPackage package, IServiceProvider services)
        {
            // Switch to the main thread - the call to AddCommand in PushToCloudFoundryCommand's constructor requires
            // the UI thread.
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);

            var commandService = await package.GetServiceAsync(typeof(IMenuCommandService)) as OleMenuCommandService;
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
                // Ensure project file access happens on the main thread
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

                var dte = (DTE2)await _package.GetServiceAsync(typeof(DTE));
                Assumes.Present(dte);

                var activeProjects = (Array)dte.ActiveSolutionProjects;

                foreach (Project proj in activeProjects)
                {
                    var projectDirectory = Path.GetDirectoryName(proj.FullName);

                    var tfm = "netstandard2.1"; // fallback value
                    try
                    {
                        tfm = proj.Properties.Item("FriendlyTargetFramework").Value.ToString();
                    }
                    catch (ArgumentException)
                    {
                        tfm = proj.Properties.Item("TargetFrameworkMoniker").Value.ToString();
                    }
                    finally
                    {
                        if (tfm == null)
                        {
                            _dialogService.DisplayWarningDialog(
                                "Unable to identify target framework",
                                "Proceeding with default target framework 'netstandard2.1'.\n" +
                                "If this is not intended and the issue persists, please reach out to tas-vs-extension@vmware.com");
                        }

                        if (tfm.StartsWith(".NETFramework") && !File.Exists(Path.Combine(projectDirectory, "Web.config")))
                        {
                            var msg = $"This project appears to target .NET Framework; pushing it to Tanzu Application Service requires a 'Web.config' file at it's base directory, but none was found in {projectDirectory}";
                            _dialogService.DisplayErrorDialog("Unable to push to Tanzu Application Service", msg);
                        }
                        else
                        {
                            var viewModel = new DeploymentDialogViewModel(_services, proj.Name, projectDirectory, tfm);
                            var view = new DeploymentDialogView(viewModel, new ThemeService());

                            view.ShowDialog();

                            // * Actions to take after modal closes:
                            if (viewModel.DeploymentInProgress) // don't open tool window if modal was closed via "X" button
                            {
                                viewModel.OutputView.Show();
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

                _dialogService.DisplayErrorDialog("Unable to push to Tanzu Application Service", ex.Message);
            }
        }
    }
}