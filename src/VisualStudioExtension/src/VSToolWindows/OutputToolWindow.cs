using Community.VisualStudio.Toolkit;
using Community.VisualStudio.Toolkit.DependencyInjection.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.Shell;
using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Tanzu.Toolkit.ViewModels;
using Tanzu.Toolkit.VisualStudio.Views;

namespace Tanzu.Toolkit.VisualStudio.VSToolWindows
{
    public sealed class OutputToolWindow : BaseToolWindow<OutputToolWindow>
    {
        public override string GetTitle(int toolWindowId) => "Tanzu Platform Output";

        public override async Task<FrameworkElement> CreateAsync(int toolWindowId, CancellationToken cancellationToken)
        {
            var serviceProvider = await
                VS.GetServiceAsync<SToolkitServiceProvider<TanzuToolkitForVisualStudioPackage>, IToolkitServiceProvider<TanzuToolkitForVisualStudioPackage>>();
            var view = serviceProvider.GetRequiredService(typeof(IOutputView));
            return (OutputView)view;
        }

        public override Type PaneType => typeof(Pane);

        [Guid("1c563078-79b7-4b16-842f-d85ba441e92e")]
        public sealed class Pane : ToolWindowPane
        {
            protected override void OnClose()
            {
                if (Content is IView view && view.ViewModel is IOutputViewModel viewModel)
                {
                    viewModel.CancelActiveProcess();
                }

                Dispose();
                base.OnClose();
            }
        }
    }
}