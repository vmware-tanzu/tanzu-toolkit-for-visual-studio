using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Serilog;
using System;
using Tanzu.Toolkit.Services.Logging;
using Tanzu.Toolkit.WpfViews;
using Tanzu.Toolkit.WpfViews.Services;

namespace Tanzu.Toolkit.VisualStudio
{
    public class VsToolWindowService : IViewService
    {
        private readonly AsyncPackage package;
        private readonly ILogger logger;

        public VsToolWindowService(AsyncPackage package, ILoggingService loggingService)
        {
            this.package = package;

            logger = loggingService.Logger;
        }

        public void DisplayViewByType(Type viewType)
        {
            try
            {
                ThreadHelper.ThrowIfNotOnUIThread();

                Type toolWindowType = null;
                if (viewType == typeof(OutputView)) toolWindowType = typeof(OutputToolWindow);

                // The last flag is set to true so that if the tool window does not exists it will be created.
                ToolWindowPane window = package.FindToolWindow(toolWindowType, 0, true);

                IVsWindowFrame windowFrame = (IVsWindowFrame)window.Frame;
                Microsoft.VisualStudio.ErrorHandler.ThrowOnFailure(windowFrame.Show());
            }
            catch (Exception ex)
            {
                logger.Error($"VisualStudioService tried to open a tool window of type {viewType} but something went wrong: {ex}");
            }
        }
    }
}
