using Community.VisualStudio.Toolkit;
using Community.VisualStudio.Toolkit.DependencyInjection;
using Community.VisualStudio.Toolkit.DependencyInjection.Core;
using Microsoft.VisualStudio.Shell;
using Tanzu.Toolkit.VisualStudio.VSToolWindows;
using Task = System.Threading.Tasks.Task;

namespace Tanzu.Toolkit.VisualStudio.Commands
{
    [Command(PackageGuids.guidTanzuToolkitPackageCmdSetString, PackageIds.TanzuExplorerCommandId)]
    internal sealed class TanzuPlatformExplorerCommand : BaseDICommand
    {
        public TanzuPlatformExplorerCommand(DIToolkitPackage package)
            : base(package)
        {
        }

        protected override async Task ExecuteAsync(OleMenuCmdEventArgs e)
        {
            await TanzuExplorerToolWindow.ShowAsync();
        }
    }
}