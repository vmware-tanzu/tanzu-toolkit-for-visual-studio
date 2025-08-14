using Community.VisualStudio.Toolkit;
using Community.VisualStudio.Toolkit.DependencyInjection.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.Shell;
using System.Runtime.InteropServices;
using Tanzu.Toolkit.ViewModels;
using Tanzu.Toolkit.VisualStudio.Views;

namespace Tanzu.Toolkit.VisualStudio.VSToolWindows
{
    [Guid("1c563078-79b7-4b16-842f-d85ba441e92e")]
    public sealed class OutputToolWindow : ToolWindowPane
    {
        private readonly IOutputView _view;

        public OutputToolWindow() : base(null)
        {
            Caption = "Tanzu Platform Output";
            var serviceProvider = VS.GetRequiredService<SToolkitServiceProvider<TanzuToolkitForVisualStudioPackage>, IToolkitServiceProvider<TanzuToolkitForVisualStudioPackage>>();
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