using Serilog;
using System;
using System.Runtime.CompilerServices;
using Tanzu.Toolkit.Services.Logging;
using Tanzu.Toolkit.Services.ViewLocator;
using Tanzu.Toolkit.ViewModels;
using Tanzu.Toolkit.VisualStudio.Views;

[assembly: InternalsVisibleTo("Tanzu.Toolkit.VisualStudioExtension.Tests")]

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

                if (ViewShownAsToolWindow(type))
                {
                    view = _toolWindowService.CreateToolWindowForView(type, parameter as string);
                }
                else if (ViewShownAsModal(type))
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
                else
                {
                    _logger?.Error("{ClassName}.{MethodName} given type not classified as either modal or tool window: {ViewModelName}", nameof(VsViewLocatorService), nameof(GetViewByViewModelName), viewModelName);
                }

                return view;
            }
            catch (Exception ex)
            {
                _logger.Error("VsViewLocatorService encountered an error while looking for a view to match {ViewModelName}: {ViewLookupException}", viewModelName, ex);
                return view;
            }
        }

        internal bool ViewShownAsModal(Type type)
        {
            return type == typeof(ILoginView)
                || type == typeof(IDeploymentDialogView)
                || type == typeof(IAppDeletionConfirmationView)
                || type == typeof(IRemoteDebugView);

            /** ^ this solution is admittedly not ideal because it will require updating when adding/changing modals.
             * A preferred solution is shown below, but for some unknown reason the reference to AbstractModel prompts
             * an error when running tests *specifically in VS 2019* (tests pass in VS 2022):
             *      System.IO.FileNotFoundException: Could not load file or assembly 'PresentationFramework, 
             *      Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35'. The system cannot find the 
             *      file specified.
             * This error is bizarre because the PresentationFramework assembly (v4.0.0.0)  is definitely present
             * 
             *      var expectedImplementationType = Type.GetType(type.Namespace + "." + type.Name.Substring(1));
             *      return expectedImplementationType != null && expectedImplementationType.IsSubclassOf(typeof(AbstractModal));
             */

        }

        internal bool ViewShownAsToolWindow(Type type)
        {
            return type == typeof(IOutputView);
        }

        protected virtual Type LookupViewType(string viewTypeName)
        {
            return Type.GetType(_viewNamespace + "." + viewTypeName);
        }

        internal string GetViewName(string viewModelName)
        {
            return "I" + viewModelName.Substring(0, viewModelName.Length - 5);  // prepend I and remove "Model"
        }
    }
}
