using Community.VisualStudio.Toolkit.DependencyInjection;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Serilog;
using System;
using System.Threading;
using System.Threading.Tasks;
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

        public VsToolWindowService(DIToolkitPackage package, ILoggingService loggingService)
        {
            _package = package;
            _logger = loggingService.Logger;
        }

        public async Task<IView> CreateToolWindowForViewAsync(Type viewType, string caption)
        {
            IView view = null;
            Type toolWindowType = null;
            try
            {
                var id = _largestId;
                _largestId +=
                    1; // this might cause a problem after this instance of VsToolWindowService has constructed its 2,147,483,647th tool window

                if (viewType == typeof(OutputView) || viewType == typeof(IOutputView))
                {
                    toolWindowType = typeof(OutputToolWindow.Pane);
                }
                var window = await _package.FindToolWindowAsync(toolWindowType, id, true, CancellationToken.None);
                if (window?.Frame == null)
                {
                    throw new NotSupportedException("Cannot create tool window");
                }

                if (view == null)
                {
                    view = window.Content as IView;
                }

                window.Caption = caption;

                // give view a way to display its corresponding tool window
                view.DisplayView = () =>
                {
                    ThreadHelper.ThrowIfNotOnUIThread();
                    var windowFrame = (IVsWindowFrame)window.Frame;
                    Microsoft.VisualStudio.ErrorHandler.ThrowOnFailure(windowFrame.Show());
                };

                return view;
            }
            catch (Exception ex)
            {
                _logger.Error(
                    "VsToolWindowService tried to open a tool window of type {ViewType} but something went wrong: {ToolWindowDisplayException}",
                    viewType, ex);
                return view;
            }
        }
    }
}