using EnvDTE;
using EnvDTE80;
using Microsoft;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.Shell;
using Serilog;
using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.IO;
using Tanzu.Toolkit.Services.ErrorDialog;
using Tanzu.Toolkit.Services.File;
using Tanzu.Toolkit.Services.Logging;
using Task = System.Threading.Tasks.Task;

namespace Tanzu.Toolkit.VisualStudio.Commands
{
    /// <summary>
    /// Command handler.
    /// </summary>
    internal sealed class OpenLogsCommand
    {
        /// <summary>
        /// Command ID.
        /// </summary>
        public const int _commandId = 259;

        /// <summary>
        /// Command menu group (command set GUID).
        /// </summary>
        public static readonly Guid _commandSet = new Guid("f91c88fb-6e17-42a6-878d-f4d16ead7625");

        public static DTE2 Dte { get; private set; }
        private static IFileService _fileService;
        private static ILogger _logger;
        private static IErrorDialog _dialogService;

        /// <summary>
        /// Initializes a new instance of the <see cref="OpenLogsCommand"/> class.
        /// Adds our command handlers for menu (commands must exist in the command table file).
        /// </summary>
        /// <param name="commandService">Command service to add command to, not null.</param>
        /// 
        private OpenLogsCommand(OleMenuCommandService commandService)
        {
            commandService = commandService ?? throw new ArgumentNullException(nameof(commandService));
            var menuCommandID = new CommandID(_commandSet, _commandId);
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

        /// <summary>
        /// Initializes the command.
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        /// <param name="services">IServiceProvider used to lookup auxiliary services.</param>
        public static async Task InitializeAsync(AsyncPackage package, IServiceProvider services)
        {
            // Switch to the main thread - the call to AddCommand in OpenLogsCommand's constructor requires
            // the UI thread.
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);

            var commandService = await package.GetServiceAsync(typeof(IMenuCommandService)) as OleMenuCommandService;
            Instance = new OpenLogsCommand(commandService);

            Dte = await package.GetServiceAsync(typeof(DTE)) as DTE2;
            _fileService = services.GetRequiredService<IFileService>();
            _dialogService = services.GetRequiredService<IErrorDialog>();
            var logSvc = services.GetRequiredService<ILoggingService>();

            Assumes.Present(Dte);
            Assumes.Present(_fileService);
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

            var logFilePath = _fileService.PathToLogsFile;
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
                    _logger.Error("An error occurred in OpenLogsCommand while trying to generate a tmp log file to display: {OpenLogsCommandException}", ex);
                    _dialogService.DisplayErrorDialog("Unable to open log file.", ex.Message);
                }

                if (tmpFilePath != null)
                {
                    var logsWindow = Dte.ItemOperations.OpenFile(tmpFilePath);
                    logsWindow.Document.ReadOnly = true;

                    var windowEvents = Dte.Events.get_WindowEvents(logsWindow);
                    windowEvents.WindowClosing += OnWindowClosing;

                    void OnWindowClosing(Window window)
                    {
                        try
                        {
                            RemoveTmpLogFile(tmpFilePath);
                        }
                        catch (Exception ex)
                        {
                            _logger.Error("Unable to delete tmp log file '{TmpFilePath}'.\n{CloseLogsWindowException}", tmpFilePath, ex);
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

            var logContents = File.ReadAllLines(tempFilePath);

            var extraInfo = new List<string>
            {
                "==========================",
                "This is a temporary copy of the log file located at:",
                originalFilePath,
                $"This file was created {DateTime.Now}; any logs recorded since then will not appear in this file.",
                "This file will be deleted from the file system when this window is closed.",
                "==========================",
            };

            // add explanation to the BEGINNING of the temp file
            var newContents = extraInfo;
            foreach (var s in logContents)
            {
                newContents.Add(s);
            }

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
