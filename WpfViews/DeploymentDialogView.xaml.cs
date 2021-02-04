using System.Windows.Controls;
using System.Windows.Input;
using Tanzu.Toolkit.VisualStudio.ViewModels;
using Tanzu.Toolkit.VisualStudio.WpfViews.Commands;

namespace Tanzu.Toolkit.VisualStudio.WpfViews
{
    /// <summary>
    /// Interaction logic for DeploymentDialogView.xaml
    /// </summary>
    public partial class DeploymentDialogView : UserControl, IDeploymentDialogView
    {
        private IDeploymentDialogViewModel _viewModel;
        public ICommand UploadAppCommand { get; }
        public ICommand OpenLoginDialogCommand { get; }

        public DeploymentDialogView()
        {
            InitializeComponent();
        }

        public DeploymentDialogView(IDeploymentDialogViewModel viewModel)
        {
            _viewModel = viewModel;
            UploadAppCommand = new DelegatingCommand(viewModel.DeployApp, viewModel.CanDeployApp);
            OpenLoginDialogCommand = new DelegatingCommand(viewModel.OpenLoginView, viewModel.CanOpenLoginView);

            DataContext = viewModel;
            InitializeComponent();
        }

        private void CfInstanceOptions_ComboBox_DropDownClosed(object sender, System.EventArgs e)
        {
            _viewModel.UpdateCfOrgOptions();
        }

        private void CfOrgOptions_ComboBox_DropDownClosed(object sender, System.EventArgs e)
        {
            _viewModel.UpdateCfSpaceOptions();
        }

        private void DeploymentStatus_TextChanged(object sender, TextChangedEventArgs e)
        {
            deploymentStatusText.ScrollToEnd();
        }

    }
}