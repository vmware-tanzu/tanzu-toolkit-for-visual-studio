using Community.VisualStudio.Toolkit;
using Community.VisualStudio.Toolkit.DependencyInjection.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.Shell;
using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Tanzu.Toolkit.VisualStudio.Views;

namespace Tanzu.Toolkit.VisualStudio.VSToolWindows
{
    public class TanzuExplorerToolWindow : BaseToolWindow<TanzuExplorerToolWindow>
    {
        public override string GetTitle(int toolWindowId) => "Tanzu Platform Explorer";

        public override async Task<FrameworkElement> CreateAsync(int toolWindowId, CancellationToken cancellationToken)
        {
            var serviceProvider = await
                VS.GetServiceAsync<SToolkitServiceProvider<TanzuToolkitForVisualStudioPackage>, IToolkitServiceProvider<TanzuToolkitForVisualStudioPackage>>();
            var view = serviceProvider.GetRequiredService(typeof(ITanzuExplorerView));
            return (TanzuExplorerView)view;
        }

        public override Type PaneType => typeof(Pane);

        [Guid("051b6546-acb2-4f74-85b3-60de9fefab24")]
        public sealed class Pane : ToolWindowPane
        {
        }
    }
}