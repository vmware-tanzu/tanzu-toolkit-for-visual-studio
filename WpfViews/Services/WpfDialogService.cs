using Microsoft.Extensions.DependencyInjection;
using System;
using System.Windows;
using Tanzu.Toolkit.VisualStudio.Services.Dialog;
using Tanzu.Toolkit.VisualStudio.Services.ViewLocator;

namespace Tanzu.Toolkit.VisualStudio.WpfViews.Services
{
    public class WpfDialogService : IDialogService
    {
        public IServiceProvider ServiceProvider;
        public IViewLocatorService ViewLocatorService;

        public WpfDialogService(IServiceProvider serviceProvider)
        {
            ServiceProvider = serviceProvider;
            ViewLocatorService = ServiceProvider.GetRequiredService<IViewLocatorService>();
        }

        public void CloseDialog(object dialogWindow, bool result)
        {
            var asWindows = dialogWindow as Window;
            asWindows.DialogResult = result;
        }

        public IDialogResult ShowDialog(string dialogName, object parameter = null)
        {
            var dialog = ViewLocatorService.NavigateTo(dialogName, parameter) as DependencyObject;
            var dialogWindow = Window.GetWindow(dialog);
            var result = dialogWindow.ShowDialog();
            //dialogWindow.Parent = Application.Current.MainWindow;

            return new WpfDialogResult() { Result = result };
        }
    }
}
