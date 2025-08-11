using Microsoft.VisualStudio.PlatformUI;
using System;
using System.Windows.Input;
using Tanzu.Toolkit.ViewModels;

namespace Tanzu.Toolkit.VisualStudio.Views
{
    public class AbstractModal : DialogWindow, IView
    {
        private Action _displayView;

        public Action DisplayView
        {
            get
            {
                if (_displayView == null)
                {
                    _displayView = () => ShowModal();
                }

                return _displayView;
            }

            set => _displayView = value;
        }

        public IViewModel ViewModel => (IViewModel)DataContext;

        public void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                DragMove();
            }
        }
    }
}