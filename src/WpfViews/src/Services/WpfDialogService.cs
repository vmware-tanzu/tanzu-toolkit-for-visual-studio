using System;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Tanzu.Toolkit.Services.Dialog;
using Tanzu.Toolkit.Services.ViewLocator;
using Tanzu.Toolkit.ViewModels;

namespace Tanzu.Toolkit.WpfViews.Services
{
    public class WpfDialogService : IDialogService
    {
        private IServiceProvider _serviceProvider;
        private IViewLocatorService _viewLocatorService;

        public WpfDialogService(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            _viewLocatorService = this._serviceProvider.GetRequiredService<IViewLocatorService>();
        }

        public void CloseDialog(object dialogWindow, bool result)
        {
            var asWindows = dialogWindow as Window;
            asWindows.DialogResult = result;
        }

        public IDialogResult ShowDialog(string dialogName, object parameter = null)
        {
            var dialog = _viewLocatorService.NavigateTo(dialogName, parameter) as DependencyObject;
            var dialogWindow = Window.GetWindow(dialog);
            var result = dialogWindow.ShowDialog();
            // dialogWindow.Parent = Application.Current.MainWindow;

            return new WpfDialogResult() { Result = result };
        }

        public void DisplayErrorDialog(string errorTitle, string errorMsg)
        {
            /* Ensure dialog is displayed in UI thread */
            Application.Current.Dispatcher.Invoke(() =>
            {
                var viewModel = new ErrorDialogViewModel()
                {
                    Title = errorTitle,
                    Message = errorMsg,
                };

                var view = new ErrorDialogView(viewModel);
                view.ShowDialog();
            });
        }
    }
}
