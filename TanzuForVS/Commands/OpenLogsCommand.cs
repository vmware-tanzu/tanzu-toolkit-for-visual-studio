using EnvDTE;
using EnvDTE80;
using Microsoft;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.Shell;
using System;
using System.IO;
using System.ComponentModel.Design;
using Tanzu.Toolkit.VisualStudio.Services.FileLocator;
using Task = System.Threading.Tasks.Task;
using Tanzu.Toolkit.VisualStudio.Services.Dialog;
using System.Collections.Generic;
using Serilog;
using Tanzu.Toolkit.VisualStudio.Services.Logging;

namespace Tanzu.Toolkit.VisualStudio.Commands
{
    /// <summary>
    /// Command handler
    /// </summary>
    internal sealed class OpenLogsCommand
    {
        private static DTE2 _dte;
        private static IFileLocatorService _fileLocatorService;
        private static IDialogService _dialogService;
        private static ILogger _logger;

        /// <summary>
        /// Command ID.
        /// </summary>
        public const int CommandId = 259;

        /// <summary>
        /// Command menu group (command set GUID).
        /// </summary>
        public static readonly Guid CommandSet = new Guid("f91c88fb-6e17-42a6-878d-f4d16ead7625");

        /// <summary>
        /// VS Package that provides this command, not null.
        /// </summary>
        private readonly AsyncPackage package;

        /// <summary>
        /// Initializes a new instance of the <see cref="OpenLogsCommand"/> class.
        /// Adds our command handlers for menu (commands must exist in the command table file)
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        /// <param name="commandService">Command service to add command to, not null.</param>
        private OpenLogsCommand(AsyncPackage package, OleMenuCommandService commandService)
        {
            this.package = package ?? throw new ArgumentNullException(nameof(package));
            commandService = commandService ?? throw new ArgumentNullException(nameof(commandService));

            var menuCommandID = new CommandID(CommandSet, CommandId);
            var menuItem = new MenuCommand(this.Execute, menuCommandID);
            commandService.AddCommand(menuItem);
        }

        /// <summary>
        /// Gets the instance of the command.
        /// </summary>
        public static OpenLogsCommand Instance
        {
            get;
            private set;
        }

        public static DTE2 Dte
        {
            get => _dte;

            private set
            {
                _dte = value;
            }
        }

        /// <summary>
        /// Gets the service provider from the owner package.
        /// </summary>
        private Microsoft.VisualStudio.Shell.IAsyncServiceProvider ServiceProvider
        {
            get
            {
                return this.package;
            }
        }

        /// <summary>
        /// Initializes the singleton instance of the command.
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        public static async Task InitializeAsync(AsyncPackage package, IServiceProvider services)
        {
            // Switch to the main thread - the call to AddCommand in OpenLogsCommand's constructor requires
            // the UI thread.
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);

            OleMenuCommandService commandService = await package.GetServiceAsync(typeof(IMenuCommandService)) as OleMenuCommandService;
            Instance = new OpenLogsCommand(package, commandService);

            Dte = await package.GetServiceAsync(typeof(DTE)) as DTE2;
            _fileLocatorService = services.GetRequiredService<IFileLocatorService>();
            _dialogService = services.GetRequiredService<IDialogService>();
            var logSvc = services.GetRequiredService<ILoggingService>();

            Assumes.Present(Dte);
            Assumes.Present(_fileLocatorService);
            Assumes.Present(_dialogService);
            Assumes.Present(logSvc);

            _logger = logSvc.Logger;
        }

        /// <summary>
        /// This function is the callback used to execute the command when the menu item is clicked.
        /// See the constructor to see how the menu item is associated with this function using
        /// OleMenuCommandService service and MenuCommand class.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event args.</param>
        private void Execute(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var logFilePath = _fileLocatorService.PathToLogsFile;
            var tmpFilePath = GenerateTmpFileName(logFilePath);

            var alreadyOpen = Dte.ItemOperations.IsFileOpen(tmpFilePath);

            if (alreadyOpen)
            {
                var doc = Dte.Documents.Item(tmpFilePath);
                var win = doc.ActiveWindow;
                doc.Activate();
            }
            else
            {
                try
                {
                    CloneFile(logFilePath, tmpFilePath);
                }
                catch (Exception ex)
                {
                    _logger.Error($"An error occurred in OpenLogsCommand while trying to generate a tmp log file to display. {ex.Message}");
                    _dialogService.DisplayErrorDialog("Unable to open log file.", ex.Message);
                }

                if (tmpFilePath != null)
                {
                    Window logsWindow = Dte.ItemOperations.OpenFile(tmpFilePath);
                    logsWindow.Document.ReadOnly = true;

                    WindowEvents _windowEvents = Dte.Events.get_WindowEvents(logsWindow);
                    _windowEvents.WindowClosing += OnWindowClosing;

                    void OnWindowClosing(Window Window)
                    {
                        try
                        {
                            RemoveTmpLogFile(tmpFilePath);
                        }
                        catch (Exception ex)
                        {
                            string errorMsg = $"Unable to delete tmp log file '${tmpFilePath}'.{Environment.NewLine}{ex.Message}";

                            _logger.Error(errorMsg);
                        }
                    }
                }
            }
        }

        private string GenerateTmpFileName(string originalFilePath)
        {
            var originalName = Path.GetFileNameWithoutExtension(originalFilePath);
            return originalFilePath.Replace(originalName, $"{originalName}-recent");
        }

        private void CloneFile(string originalFilePath, string tempFilePath)
        {
            // File.Copy allows for extraction of log data without
            // conflicting with the logger's lock on the log file.
            File.Copy(originalFilePath, tempFilePath, overwrite: true);

            string[] logContents = File.ReadAllLines(tempFilePath);

            List<string> extraInfo = new List<string>{
                "==========================",
                "This is a temporary copy of the log file located at:",
                originalFilePath,
                $"This file was created {DateTime.Now}; any logs recorded since then will not appear in this file.",
                "This file will be deleted from the file system when this window is closed.",
                "=========================="
            };

            // add explanation to the BEGINNING of the temp file
            List<string> newContents = extraInfo;
            foreach (string s in logContents) newContents.Add(s);

            // remove original copy to allow extra info to be prepended
            File.Delete(tempFilePath);

            // write entire content to new file with same "temp" name
            File.AppendAllLines(tempFilePath, newContents);
        }

        private void RemoveTmpLogFile(string filePath)
        {
            File.Delete(filePath);
        }
    }
}
