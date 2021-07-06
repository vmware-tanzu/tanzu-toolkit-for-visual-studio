using System.Security;
using System.Windows;
using System.Windows.Input;
using Tanzu.Toolkit.ViewModels;
using Tanzu.Toolkit.WpfViews.Commands;
using Tanzu.Toolkit.WpfViews.ThemeService;

namespace Tanzu.Toolkit.WpfViews
{
    /// <summary>
    /// Interaction logic for AddCloudDialogView.xaml.
    /// </summary>
    public partial class AddCloudDialogView : Window, IAddCloudDialogView
    {
        public AddCloudDialogView()
        {
            InitializeComponent();
        }

        public AddCloudDialogView(IAddCloudDialogViewModel viewModel, IThemeService themeService)
        {
            AddCloudCommand = new AsyncDelegatingCommand(viewModel.AddCloudFoundryInstance, viewModel.CanAddCloudFoundryInstance);
            viewModel.GetPassword = GetPassword;
            themeService.SetTheme(this);
            DataContext = viewModel;
            InitializeComponent();
        }

        public SecureString GetPassword()
        {
            return pbPassword.SecurePassword;
        }

        public ICommand AddCloudCommand { get; }
    }
}
