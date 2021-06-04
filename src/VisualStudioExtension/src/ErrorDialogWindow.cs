using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Tanzu.Toolkit.Services.ErrorDialog;

namespace Tanzu.Toolkit.VisualStudio
{
    public class ErrorDialogWindowService : IErrorDialog
    {
        private static IServiceProvider _serviceProvider;

        public ErrorDialogWindowService(IServiceProvider services)
        {
            _serviceProvider = services;
        }

        public void DisplayErrorDialog(string errorTitle, string errorMsg)
        {
            /* Ensure dialog is displayed in UI thread */
            
           VsShellUtilities.ShowMessageBox(
                 _serviceProvider,
                 errorMsg,
                 errorTitle,
                 OLEMSGICON.OLEMSGICON_CRITICAL,
                 OLEMSGBUTTON.OLEMSGBUTTON_OK,
                 OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST
            );
            
        }
    }
}


