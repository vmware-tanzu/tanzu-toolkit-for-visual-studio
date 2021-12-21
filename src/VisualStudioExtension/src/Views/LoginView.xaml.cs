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
        public ICommand IncrementPageCommand { get; }
        public ICommand DecrementPageCommand { get; }

        public LoginView()
        {
            InitializeComponent();
        }

        public LoginView(ILoginViewModel viewModel)
        {
            System.Predicate<object> alwaysTrue = (object arg) => { return true; };

            AddCloudCommand = new AsyncDelegatingCommand(viewModel.LogIn, viewModel.CanLogIn);
            SsoCommand = new AsyncDelegatingCommand(viewModel.OpenSsoDialog, viewModel.CanOpenSsoDialog);
            IncrementPageCommand = new AsyncDelegatingCommand(viewModel.NavigateToAuthPage, viewModel.CanProceedToAuthentication);
            DecrementPageCommand = new DelegatingCommand(viewModel.NavigateToTargetPage, alwaysTrue);

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
            Hide();
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
            _viewModel.ValidateApiAddress(tbUrl.Text);
        }

    }
}
