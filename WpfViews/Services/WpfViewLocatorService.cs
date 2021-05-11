using System;
using Tanzu.Toolkit.VisualStudio.Services.ViewLocator;

namespace Tanzu.Toolkit.VisualStudio.WpfViews.Services
{
    public class WpfViewLocatorService : IViewLocatorService
    {
        private readonly string viewNamespace;

        public IServiceProvider ServiceProvider { get; }

        public string ViewNamespace => viewNamespace;

        public string CurrentView { get; private set; }

        public WpfViewLocatorService(IServiceProvider serviceProvider)
        {
            ServiceProvider = serviceProvider;
            var lastIndex = typeof(WpfViewLocatorService).Namespace.LastIndexOf('.');
            viewNamespace = typeof(WpfViewLocatorService).Namespace.Substring(0, lastIndex);
        }

        public virtual object NavigateTo(string viewModelName, object parameter = null)
        {
            var viewTypeName = GetViewName(viewModelName);
            var type = Type.GetType(viewNamespace + "." + viewTypeName);
            CurrentView = viewTypeName;
            return ServiceProvider.GetService(type);
        }

        public virtual string GetViewName(string viewModelName)
        {
            return "I" + viewModelName.Substring(0, viewModelName.Length - 5);  // prepend I and remove "Model"
        }

    }
}
