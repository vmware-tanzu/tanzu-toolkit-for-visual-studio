using System.Security;
using System.Windows;
using System.Windows.Input;
using Tanzu.Toolkit.VisualStudio.ViewModels;
using Tanzu.Toolkit.VisualStudio.WpfViews.Commands;

namespace Tanzu.Toolkit.VisualStudio.WpfViews
{
    /// <summary>
    /// Interaction logic for AddCloudDialogView.xaml
    /// </summary>
    public partial class AddCloudDialogView : Window, IAddCloudDialogView
    {
        public AddCloudDialogView()
        {
            InitializeComponent();

        }

        public AddCloudDialogView(IAddCloudDialogViewModel viewModel)
        {
            AddCloudCommand = new AsyncDelegatingCommand(viewModel.AddCloudFoundryInstance, viewModel.CanAddCloudFoundryInstance);
            viewModel.GetPassword = GetPassword;
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
