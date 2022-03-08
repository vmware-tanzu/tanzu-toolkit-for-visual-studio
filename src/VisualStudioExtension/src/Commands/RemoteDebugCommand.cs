using Community.VisualStudio.Toolkit;
using Microsoft.VisualStudio.Shell;
using System.Threading.Tasks;

namespace Tanzu.Toolkit.VisualStudio
{
    [Command(PackageGuids.guidTanzuToolkitPackageCmdSetString, PackageIds.RemoteDebugId)]
    internal sealed class RemoteDebugCommand : BaseCommand<RemoteDebugCommand>
    {
        protected override async Task ExecuteAsync(OleMenuCmdEventArgs e)
        {
            await VS.MessageBox.ShowWarningAsync("RemoteDebugCommand", "Button clicked");
        }
    }
}
