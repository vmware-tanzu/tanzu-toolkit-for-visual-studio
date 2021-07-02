﻿using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using Tanzu.Toolkit.Services.ErrorDialog;
using Tanzu.Toolkit.WpfViews.ThemeService;

namespace Tanzu.Toolkit.VisualStudio
{
    public class ErrorDialogWindowService : IErrorDialog
    {
        //private static IServiceProvider _serviceProvider;
        private readonly AsyncPackage _asyncServiceProvider;

        public ErrorDialogWindowService(AsyncPackage services)
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


