using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.Shell;
using System.Runtime.InteropServices;
using Tanzu.Toolkit.ViewModels;
using Tanzu.Toolkit.VisualStudio.Views;
using ServiceProvider = Microsoft.Extensions.DependencyInjection.ServiceProvider;

namespace Tanzu.Toolkit.VisualStudio.VSToolWindows
{
    [Guid("1c563078-79b7-4b16-842f-d85ba441e92e")]
    public sealed class OutputToolWindow : ToolWindowPane
    {
        private readonly IOutputView _view;

        public OutputToolWindow(ServiceProvider serviceProvider) : base(serviceProvider)
        {
            Caption = "Tanzu Platform Output";
            _view = serviceProvider.GetRequiredService<IOutputView>();
            Content = _view;
        }

        protected override void OnClose()
        {
            if (_view is IView view && view.ViewModel is IOutputViewModel viewModel)
            {
                viewModel.CancelActiveProcess();
            }

            base.OnClose();
        }
    }
}