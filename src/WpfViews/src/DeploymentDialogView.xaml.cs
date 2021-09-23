using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Input;
using Tanzu.Toolkit.ViewModels;
using Tanzu.Toolkit.WpfViews.Commands;
using Tanzu.Toolkit.WpfViews.ThemeService;

namespace Tanzu.Toolkit.WpfViews
{
    /// <summary>
    /// Interaction logic for DeploymentDialogView.xaml.
    /// </summary>
    public partial class DeploymentDialogView : Window, IDeploymentDialogView
    {
        private IDeploymentDialogViewModel _viewModel;
        public ICommand PushFromSourceCommand { get; }
        public ICommand PushFromBinariesCommand { get; }
        public ICommand OpenLoginDialogCommand { get; }

        public DeploymentDialogView()
        {
            InitializeComponent();
        }

        public DeploymentDialogView(IDeploymentDialogViewModel viewModel, IThemeService themeService)
        {
            _viewModel = viewModel;
            PushFromSourceCommand = new DelegatingCommand(viewModel.DeployAppFromSource, viewModel.CanDeployApp);
            PushFromBinariesCommand = new DelegatingCommand(viewModel.DeployAppFromBinaries, viewModel.CanDeployApp);
            OpenLoginDialogCommand = new DelegatingCommand(viewModel.OpenLoginView, viewModel.CanOpenLoginView);
            themeService.SetTheme(this);
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

        private void SelectManifest(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                InitialDirectory = _viewModel.ProjectDirPath,
                Filter = "YAML files (*.yaml, *.yml)|*.yaml;*.yml",
                FilterIndex = 2,
                RestoreDirectory = true
            };

            if (openFileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                _viewModel.ManifestPath = openFileDialog.FileName;
            }
        }

        private void SelectPublishDirectory(object sender, RoutedEventArgs e)
        {
            FolderBrowserDialog openFolderDialog = new FolderBrowserDialog
            {
                SelectedPath = _viewModel.ProjectDirPath,
            };

            if (openFolderDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                _viewModel.DirectoryPath = openFolderDialog.SelectedPath;
            }
        }
    }
}