﻿using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.PlatformUI;
using Serilog;
using System;
using System.Windows;
using Tanzu.Toolkit.Services.Dialog;
using Tanzu.Toolkit.Services.Logging;
using Tanzu.Toolkit.Services.ViewLocator;

namespace Tanzu.Toolkit.VisualStudio.Services
{
    public class DialogService : IDialogService
    {
        private readonly ILogger _logger;
        private readonly IViewLocatorService _viewLocatorService;

        public DialogService(IServiceProvider serviceProvider)
        {
            var loggingSvc = serviceProvider.GetRequiredService<ILoggingService>();
            _logger = loggingSvc.Logger;
            _viewLocatorService = serviceProvider.GetRequiredService<IViewLocatorService>();
        }

        public void CloseDialog(object dialogWindow, bool result)
        {
            var asWindows = dialogWindow as Window;
            asWindows?.Hide();
        }

        public IDialogResult ShowModal(string dialogName, object parameter = null)
        {
            if (!(_viewLocatorService.GetViewByViewModelName(dialogName, parameter) is DependencyObject dialog))
            {
                _logger?.Error("{ClassName} failed to show dialog for {DialogName}; {MethodName} returned null", nameof(DialogService), dialogName, nameof(_viewLocatorService.GetViewByViewModelName));
                return null;
            }

            if (Window.GetWindow(dialog) is DialogWindow dialogWindow)
            {
                var result = dialogWindow.ShowModal();
                return new DialogResult { Result = result };
            }

            return null;
        }

        public void CloseDialogByName(string dialogName, object parameter = null)
        {
            var dialog = _viewLocatorService.GetViewByViewModelName(dialogName, parameter) as DependencyObject;
            var dialogWindow = Window.GetWindow(dialog);
            dialogWindow?.Hide();
        }
    }

    public class DialogResult : IDialogResult
    {
        public bool? Result { get; set; }
    }
}
