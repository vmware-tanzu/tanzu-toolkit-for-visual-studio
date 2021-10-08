using System.Security;
using System.Windows;
using System.Windows.Input;
using Tanzu.Toolkit.ViewModels;
using Tanzu.Toolkit.WpfViews.Commands;
using Tanzu.Toolkit.WpfViews.ThemeService;

namespace Tanzu.Toolkit.WpfViews
{
    /// <summary>
    /// Interaction logic for LoginView.xaml.
    /// </summary>
    public partial class LoginView : Window, ILoginView
    {
        public LoginView()
        {
            InitializeComponent();
        }

        public LoginView(ILoginViewModel viewModel, IThemeService themeService)
        {
            AddCloudCommand = new AsyncDelegatingCommand(viewModel.LogIn, viewModel.CanLogIn);
            viewModel.GetPassword = GetPassword;
            viewModel.PasswordEmpty = PasswordBoxEmpty;
            themeService.SetTheme(this);
            DataContext = viewModel;
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

        public ICommand AddCloudCommand { get; }

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
    }
}
