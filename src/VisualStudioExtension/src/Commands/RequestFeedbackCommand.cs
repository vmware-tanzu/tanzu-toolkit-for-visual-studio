using Microsoft.VisualStudio.Shell;
using System;
using System.ComponentModel.Design;
using Task = System.Threading.Tasks.Task;

namespace Tanzu.Toolkit.VisualStudio
{
    /// <summary>
    /// Command handler
    /// </summary>
    internal sealed class RequestFeedbackCommand
    {
        /// <summary>
        /// Command ID.
        /// </summary>
        public const int _commandId = 258;

        /// <summary>
        /// Command menu group (command set GUID).
        /// </summary>
        public static readonly Guid _commandSet = new Guid("f91c88fb-6e17-42a6-878d-f4d16ead7625");

        /// <summary>
        /// Initializes a new instance of the <see cref="RequestFeedbackCommand"/> class.
        /// Adds our command handlers for menu (commands must exist in the command table file)
        /// </summary>
        /// <param name="commandService">Command service to add command to, not null.</param>
        /// 
        private RequestFeedbackCommand(OleMenuCommandService commandService)
        {
            commandService = commandService ?? throw new ArgumentNullException(nameof(commandService));
            var menuCommandID = new CommandID(_commandSet, _commandId);
            var menuItem = new MenuCommand(this.Execute, menuCommandID);
            commandService.AddCommand(menuItem);
        }

        /// <summary>
        /// Gets the instance of the command.
        /// </summary>
        public static RequestFeedbackCommand Instance
        {
            get;
            private set;
        }

        /// <summary>
        /// Initializes the singleton instance of the command.
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        public static async Task InitializeAsync(AsyncPackage package)
        {
            // Switch to the main thread - the call to AddCommand in RequestFeedbackCommand's constructor requires
            // the UI thread.
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);

            var commandService = await package.GetServiceAsync(typeof(IMenuCommandService)) as OleMenuCommandService;
            Instance = new RequestFeedbackCommand(commandService);
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
            var mailto = string.Format("mailto:{0}?Subject={1}", "tas-vs-extension@vmware.com", "Subject of message");
            mailto = Uri.EscapeUriString(mailto);
            System.Diagnostics.Process.Start(mailto);
        }
    }
}
