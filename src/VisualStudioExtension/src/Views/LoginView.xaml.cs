using Microsoft.VisualStudio.PlatformUI;
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
        private ILoginViewModel _viewModel;
        public ICommand AddCloudCommand { get; }
        public ICommand SsoCommand { get; }
        public ICommand IncrementPageCommand { get; }
        public ICommand DecrementPageCommand { get; }

        public Brush HyperlinkBrush { get { return (Brush)GetValue(HyperlinkBrushProperty); } set { SetValue(HyperlinkBrushProperty, value); } }

        public static readonly DependencyProperty HyperlinkBrushProperty = DependencyProperty.Register("HyperlinkBrush", typeof(Brush), typeof(LoginView), new PropertyMetadata(default(Brush)));

        public LoginView()
        {
            InitializeComponent();
        }

        public LoginView(ILoginViewModel viewModel, IThemeService themeService)
        {
            System.Predicate<object> alwaysTrue = (object arg) => { return true; };

            AddCloudCommand = new AsyncDelegatingCommand(viewModel.LogIn, viewModel.CanLogIn);
            SsoCommand = new AsyncDelegatingCommand(viewModel.OpenSsoDialog, viewModel.CanOpenSsoDialog);
            IncrementPageCommand = new AsyncDelegatingCommand(viewModel.NavigateToAuthPage, viewModel.CanProceedToAuthentication);
            DecrementPageCommand = new DelegatingCommand(viewModel.NavigateToTargetPage, alwaysTrue);

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
