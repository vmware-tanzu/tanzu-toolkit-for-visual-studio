using Serilog;
using System;
using Tanzu.Toolkit.Services.Logging;
using Tanzu.Toolkit.Services.ViewLocator;
using Tanzu.Toolkit.VisualStudio.Views;

namespace Tanzu.Toolkit.VisualStudio.Services
{
    public class VsViewLocatorService : IViewLocatorService
    {
        private readonly string _viewNamespace;
        private IToolWindowService _toolWindowService;
        private ILogger _logger;

        public IServiceProvider ServiceProvider { get; }

        public VsViewLocatorService(IToolWindowService toolWindowService, ILoggingService loggingService, IServiceProvider serviceProvider)
        {
            ServiceProvider = serviceProvider;
            var lastIndex = typeof(VsViewLocatorService).Namespace.LastIndexOf('.');
            _viewNamespace = typeof(VsViewLocatorService).Namespace.Substring(0, lastIndex) + ".Views"; // Tanzu.Toolkit.VisualStudio.Services -> Tanzu.Toolkit.VisualStudio.Views
            _toolWindowService = toolWindowService;
            _logger = loggingService.Logger;
        }

        public virtual object GetViewByViewModelName(string viewModelName, object parameter = null)
        {
            object view = null;
            try
            {
                var viewTypeName = GetViewName(viewModelName);
                Type type = LookupViewType(viewTypeName);

                if (type == typeof(IOutputView))
                {
                    view = _toolWindowService.CreateToolWindowForView(type, parameter as string);
                }
                else
                {
                    view = ServiceProvider.GetService(type);
                }

                return view;
            }
            catch (Exception ex)
            {
                _logger.Error("VsViewLocatorService encountered an error while looking for a view to match {ViewModelName}: {ViewLookupException}", viewModelName, ex);
                return view;
            }
        }

        protected virtual Type LookupViewType(string viewTypeName)
        {
            return Type.GetType(_viewNamespace + "." + viewTypeName);
        }

        private string GetViewName(string viewModelName)
        {
            return "I" + viewModelName.Substring(0, viewModelName.Length - 5);  // prepend I and remove "Model"
        }
    }
}
