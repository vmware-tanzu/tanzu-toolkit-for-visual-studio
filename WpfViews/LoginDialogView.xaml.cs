using System.Security;
using System.Windows;
using System.Windows.Input;
using TanzuForVS.ViewModels;
using TanzuForVS.WpfViews.Commands;

namespace TanzuForVS.WpfViews
{
    /// <summary>
    /// Interaction logic for LoginDialogView.xaml
    /// </summary>
    public partial class LoginDialogView : Window, ILoginDialogView
    {
        public LoginDialogView()
        {
            InitializeComponent();

        }

        public LoginDialogView(ILoginDialogViewModel viewModel)
        {
            LoginCommand = new AsyncDelegatingCommand(viewModel.ConnectToCloudFoundry, viewModel.CanConnectToCloudFoundry);
            viewModel.GetPassword = GetPassword;
            DataContext = viewModel;
            InitializeComponent();
        }

        public SecureString GetPassword()
        {
            return pbPassword.SecurePassword;
        }

        public ICommand LoginCommand { get; }
    }
}
