using System;
using Tanzu.Toolkit.Services.ViewLocator;

namespace Tanzu.Toolkit.VisualStudio.Services
{
    public class VsViewLocatorService : IViewLocatorService
    {
        private readonly string _viewNamespace;

        public IServiceProvider ServiceProvider { get; }

        public string ViewNamespace => _viewNamespace;

        public string CurrentView { get; private set; }

        public VsViewLocatorService(IServiceProvider serviceProvider)
        {
            ServiceProvider = serviceProvider; var lastIndex = typeof(VsViewLocatorService).Namespace.LastIndexOf('.');
            _viewNamespace = typeof(VsViewLocatorService).Namespace.Substring(0, lastIndex) + ".Views";
        }

        public virtual object NavigateTo(string viewModelName, object parameter = null)
        {
            var viewTypeName = GetViewName(viewModelName);
            var type = Type.GetType(_viewNamespace + "." + viewTypeName);
            CurrentView = viewTypeName;
            var service = ServiceProvider.GetService(type);
            return service;
        }

        public virtual string GetViewName(string viewModelName)
        {
            return "I" + viewModelName.Substring(0, viewModelName.Length - 5);  // prepend I and remove "Model"
        }
    }
}
