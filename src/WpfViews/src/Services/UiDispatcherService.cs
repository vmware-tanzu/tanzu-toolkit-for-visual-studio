using System;
using System.Windows;
using Tanzu.Toolkit.Services;

namespace Tanzu.Toolkit.VisualStudio.WpfViews.Services
{
    public class UiDispatcherService : IUiDispatcherService
    {
        public void RunOnUiThread(Action method)
        {
            Application.Current.Dispatcher.Invoke(method);
        }
    }
}
