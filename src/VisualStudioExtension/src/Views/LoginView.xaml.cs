using Microsoft.VisualStudio.PlatformUI;
using System.Diagnostics;
using System.Security;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using Tanzu.Toolkit.ViewModels;
using Tanzu.Toolkit.VisualStudio.Services;
using Tanzu.Toolkit.VisualStudio.Views.Commands;

namespace Tanzu.Toolkit.VisualStudio.Views
{
    /// <summary>
    /// Interaction logic for LoginView.xaml.
    /// </summary>
    public partial class LoginView : DialogWindow, ILoginView
    {
        private readonly ILoginViewModel _viewModel;
        public ICommand AddCloudCommand { get; }
        public ICommand SsoCommand { get; }
        public ICommand ProceedToAuthenticationCommand { get; }
        public ICommand DecrementPageCommand { get; }
        public ICommand LogInWithPasscodeCommand { get; }

        public Brush HyperlinkBrush { get { return (Brush)GetValue(_hyperlinkBrushProperty); } set { SetValue(_hyperlinkBrushProperty, value); } }

        public static readonly DependencyProperty _hyperlinkBrushProperty = DependencyProperty.Register("HyperlinkBrush", typeof(Brush), typeof(LoginView), new PropertyMetadata(default(Brush)));

        public LoginView()
        {
            InitializeComponent();
        }

        public LoginView(ILoginViewModel viewModel, IThemeService themeService)
        {
            bool alwaysTrue(object arg) { return true; }

            AddCloudCommand = new AsyncDelegatingCommand(viewModel.LogIn, viewModel.CanLogIn);
            SsoCommand = new DelegatingCommand(viewModel.ShowSsoLogin, alwaysTrue);
            ProceedToAuthenticationCommand = new AsyncDelegatingCommand(viewModel.ConnectToCf, viewModel.CanProceedToAuthentication);
            DecrementPageCommand = new DelegatingCommand(viewModel.DecrementPageNum, alwaysTrue);
            LogInWithPasscodeCommand = new AsyncDelegatingCommand(viewModel.LoginWithSsoPasscodeAsync, alwaysTrue);

            viewModel.GetPassword = GetPassword;
            viewModel.PasswordEmpty = PasswordBoxEmpty;
            viewModel.ClearPassword = ClearPassword;
            DataContext = viewModel;
            _viewModel = viewModel;

            themeService.SetTheme(this);

            InitializeComponent();

            MouseDown += Window_MouseDown;
        }

        public SecureString GetPassword()
        {
            return pbPassword.SecurePassword;
        }

        public void ClearPassword()
        {
            pbPassword.Clear();
        }

        public bool PasswordBoxEmpty()
        {
            return string.IsNullOrWhiteSpace(pbPassword.Password);
        }

        private void Close(object sender, RoutedEventArgs e)
        {
            Hide();
            _viewModel.DecrementPageNum();
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                DragMove();
            }
        }

        private void TbUrl_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            _viewModel.TargetApiAddress = tbUrl.Text; // update property *before* focus is lost from text box 
            _viewModel.ValidateApiAddressFormat(tbUrl.Text);
            _viewModel.ResetTargetDependentFields();
        }

        private void Hyperlink_RequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
        {
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri));
            e.Handled = true;
        }
    }
}
