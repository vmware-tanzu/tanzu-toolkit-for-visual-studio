using Serilog;
using System;
using System.Windows;
using Tanzu.Toolkit.Services.Logging;
using Tanzu.Toolkit.Services.ViewLocator;
using Tanzu.Toolkit.ViewModels;
using Tanzu.Toolkit.VisualStudio.Views;

namespace Tanzu.Toolkit.VisualStudio.Services
{
    public class VsViewLocatorService : IViewLocatorService
    {
        private readonly string _viewNamespace;
        private readonly IToolWindowService _toolWindowService;
        private readonly ILogger _logger;

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
            IView view = null;
            try
            {
                var viewTypeName = GetViewName(viewModelName);
                var type = LookupViewType(viewTypeName);

                if (type == typeof(IOutputView))
                {
                    view = _toolWindowService.CreateToolWindowForView(type, parameter as string);
                }
                else
                {
                    try
                    {
                        view = ServiceProvider.GetService(type) as IView;
                    }
                    catch (Exception ex)
                    {
                        _logger?.Error("Caught exception in {ClassName}.{MethodName}({ViewModelName}); either the service was unattainable or could not be cast as {CastType}: {ServiceException}", nameof(VsViewLocatorService), nameof(GetViewByViewModelName), viewModelName, nameof(IView), ex);
                    }
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
