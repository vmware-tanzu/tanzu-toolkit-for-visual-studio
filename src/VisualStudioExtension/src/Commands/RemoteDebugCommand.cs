using Community.VisualStudio.Toolkit;
using Microsoft.VisualStudio.Shell;
using System.Threading.Tasks;

namespace Tanzu.Toolkit.VisualStudio
{
    [Command("f91c88fb-6e17-42a6-878d-f4d16ead7625", 260)]
    internal sealed class RemoteDebugCommand : BaseCommand<RemoteDebugCommand>
    {
        protected override async Task ExecuteAsync(OleMenuCmdEventArgs e)
        {
            await VS.MessageBox.ShowWarningAsync("RemoteDebugCommand", "Button clicked");
        }
    }
}
