using Microsoft.VisualStudio.PlatformUI;
using System.Windows;
using System.Windows.Input;
using Tanzu.Toolkit.ViewModels.SsoDialog;
using Tanzu.Toolkit.VisualStudio.Views.Commands;

namespace Tanzu.Toolkit.VisualStudio.Views
{
    /// <summary>
    /// Interaction logic for SsoDialog.xaml
    /// </summary>
    public partial class SsoDialogView : DialogWindow, ISsoDialogView
    {
        public ICommand LogInWithPasscodeCommand { get; }

        public SsoDialogView(ISsoDialogViewModel viewModel)
        {
            LogInWithPasscodeCommand = new AsyncDelegatingCommand(viewModel.LoginWithPasscodeAsync, viewModel.CanLoginWithPasscode);

            DataContext = viewModel;

            InitializeComponent();

            MouseDown += Window_MouseDown;
        }

        private void Close(object sender, RoutedEventArgs e)
        {
            Hide();
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                DragMove();
            }
        }
    }
}
