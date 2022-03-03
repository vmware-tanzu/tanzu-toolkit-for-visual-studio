using Microsoft.VisualStudio.PlatformUI;
using System.Diagnostics;
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

        private void Hyperlink_RequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
        {
            // for .NET Core you need to add UseShellExecute = true
            // see https://docs.microsoft.com/dotnet/api/system.diagnostics.processstartinfo.useshellexecute#property-value
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri));
            e.Handled = true;
        }
    }
}
