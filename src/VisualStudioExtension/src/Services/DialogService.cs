using Microsoft.Extensions.DependencyInjection;
using System;
using System.Windows;
using Tanzu.Toolkit.Services.Dialog;
using Tanzu.Toolkit.Services.ViewLocator;

namespace Tanzu.Toolkit.VisualStudio.Services
{
    public class DialogService : IDialogService
    {
        private IServiceProvider _serviceProvider;
        private IViewLocatorService _viewLocatorService;

        public DialogService(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            _viewLocatorService = _serviceProvider.GetRequiredService<IViewLocatorService>();
        }

        public void CloseDialog(object dialogWindow, bool result)
        {
            var asWindows = dialogWindow as Window;
            asWindows.Hide();
        }

        public IDialogResult ShowDialog(string dialogName, object parameter = null)
        {
            var dialog = _viewLocatorService.GetViewByViewModelName(dialogName, parameter) as DependencyObject;
            var dialogWindow = Window.GetWindow(dialog);
            var result = dialogWindow.ShowDialog();
            return new DialogResult() { Result = result };
        }

        public void CloseDialogByName(string dialogName, object parameter = null)
        {
            var dialog = _viewLocatorService.GetViewByViewModelName(dialogName, parameter) as DependencyObject;
            var dialogWindow = Window.GetWindow(dialog);
            dialogWindow.Hide();
        }
    }

    public class DialogResult : IDialogResult
    {
        public bool? Result { get; set; }
    }
}
