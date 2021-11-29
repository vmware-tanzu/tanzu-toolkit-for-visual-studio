using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Tanzu.Toolkit.Services.ErrorDialog;

namespace Tanzu.Toolkit.VisualStudio.Services
{
    public class ErrorDialogService : IErrorDialog
    {
        //private static IServiceProvider _serviceProvider;
        private readonly AsyncPackage _asyncServiceProvider;

        public ErrorDialogService(AsyncPackage services)
        {
            _asyncServiceProvider = services;
        }

        public void DisplayErrorDialog(string errorTitle, string errorMsg)
        {
            /* Ensure dialog is displayed in UI thread */

            VsShellUtilities.ShowMessageBox(
                  _asyncServiceProvider,
                  errorMsg,
                  errorTitle,
                  OLEMSGICON.OLEMSGICON_CRITICAL,
                  OLEMSGBUTTON.OLEMSGBUTTON_OK,
                  OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST
             );

        }
    }
}


