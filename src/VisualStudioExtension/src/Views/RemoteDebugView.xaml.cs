using Microsoft.VisualStudio.PlatformUI;
using System.Windows;
using System.Windows.Input;
using Tanzu.Toolkit.ViewModels.RemoteDebug;
using Tanzu.Toolkit.VisualStudio.Services;
using Tanzu.Toolkit.VisualStudio.Views.Commands;

namespace Tanzu.Toolkit.VisualStudio
{
    /// <summary>
    /// Interaction logic for RemoteDebugView.xaml
    /// </summary>
    public partial class RemoteDebugView : DialogWindow, IRemoteDebugView
    {
        public ICommand ConfirmAppToDebugCommand { get; }

        public RemoteDebugView(IRemoteDebugViewModel viewModel, IThemeService themeService)
        {
            themeService.SetTheme(this);
            DataContext = viewModel;
            ConfirmAppToDebugCommand = new DelegatingCommand(viewModel.ConfirmAppToDebug, viewModel.CanConfirmAppToDebug);
            MouseDown += Window_MouseDown;
            InitializeComponent();
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                DragMove();
            }
        }

        private void Close(object sender, RoutedEventArgs e)
        {
            Hide();
        }
    }
}
