using Microsoft.VisualStudio.PlatformUI;
using System.Security;
using System.Windows;
using System.Windows.Input;
using Tanzu.Toolkit.ViewModels;
using Tanzu.Toolkit.VisualStudio.Views.Commands;

namespace Tanzu.Toolkit.VisualStudio.Views
{
    /// <summary>
    /// Interaction logic for LoginView.xaml.
    /// </summary>
    public partial class LoginView : DialogWindow, ILoginView
    {
        private ILoginViewModel _viewModel;
        public ICommand AddCloudCommand { get; }
        public ICommand SsoCommand { get; }


        public LoginView()
        {
            InitializeComponent();
        }

        public LoginView(ILoginViewModel viewModel)
        {
            AddCloudCommand = new AsyncDelegatingCommand(viewModel.LogIn, viewModel.CanLogIn);
            SsoCommand = new AsyncDelegatingCommand(viewModel.OpenSsoDialog, viewModel.CanOpenSsoDialog);
            viewModel.GetPassword = GetPassword;
            viewModel.PasswordEmpty = PasswordBoxEmpty;
            DataContext = viewModel;
            _viewModel = viewModel;
            InitializeComponent();

            MouseDown += Window_MouseDown;
        }

        public SecureString GetPassword()
        {
            return pbPassword.SecurePassword;
        }

        public bool PasswordBoxEmpty()
        {
            return string.IsNullOrWhiteSpace(pbPassword.Password);
        }

        private void Close(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                DragMove();
            }
        }

        private void TbUrl_LostFocus(object sender, RoutedEventArgs e)
        {
            _viewModel.VerifyApiAddress(tbUrl.Text);
        }
    }
}
