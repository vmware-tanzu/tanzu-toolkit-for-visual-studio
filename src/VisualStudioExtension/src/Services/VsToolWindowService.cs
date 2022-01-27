using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Serilog;
using System;
using System.Collections.Generic;
using Tanzu.Toolkit.Services.Logging;
using Tanzu.Toolkit.ViewModels;
using Tanzu.Toolkit.VisualStudio.Views;
using Tanzu.Toolkit.VisualStudio.VSToolWindows;

namespace Tanzu.Toolkit.VisualStudio.Services
{
    public class VsToolWindowService : IToolWindowService
    {
        private readonly AsyncPackage _package;
        private readonly ILogger _logger;
        private int _largestId = 1;

        public VsToolWindowService(AsyncPackage package, ILoggingService loggingService)
        {
            _package = package;
            _logger = loggingService.Logger;
        }

        public object CreateToolWindowForView(Type viewType, string caption)
        {
            IView view = null;
            Type toolWindowType = null;
            try
            {
                var id = _largestId;
                _largestId += 1; // this might cause a problem after this instance of VsToolWindowService has constructed its 2,147,483,647th tool window

                if (viewType == typeof(OutputView) || viewType == typeof(IOutputView))
                {
                    toolWindowType = typeof(OutputToolWindow);
                }

                ToolWindowPane window = _package.FindToolWindow(toolWindowType, id, create: true);
                if (window == null || window.Frame == null)
                {
                    throw new NotSupportedException("Cannot create tool window");
                }
                view = window.Content as IView;
                window.Caption = caption;

                // give view a way to display its corresponding tool window
                view.DisplayView = () =>
                {
                    ThreadHelper.ThrowIfNotOnUIThread();
                    IVsWindowFrame windowFrame = (IVsWindowFrame)window.Frame;
                    Microsoft.VisualStudio.ErrorHandler.ThrowOnFailure(windowFrame.Show());
                };

                return view;
            }
            catch (Exception ex)
            {
                _logger.Error("VsToolWindowService tried to open a tool window of type {ViewType} but something went wrong: {ToolWindowDisplayException}", viewType, ex);
                return view;
            }
        }
    }
}
