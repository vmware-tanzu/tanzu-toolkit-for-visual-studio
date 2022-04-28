using Microsoft.VisualStudio.PlatformUI;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using Tanzu.Toolkit.ViewModels;
using Tanzu.Toolkit.VisualStudio.Services;
using Tanzu.Toolkit.VisualStudio.Views.Commands;

namespace Tanzu.Toolkit.VisualStudio.Views
{
    /// <summary>
    /// Interaction logic for DeploymentDialogView.xaml.
    /// </summary>
    public partial class DeploymentDialogView : DialogWindow, IDeploymentDialogView
    {
        private readonly IDeploymentDialogViewModel _viewModel;
        public ICommand UploadAppCommand { get; }
        public ICommand OpenLoginDialogCommand { get; }
        public ICommand ToggleAdvancedOptionsCommand { get; }
        public ICommand ClearBuildpackSelectionCommand { get; }
        public DelegatingCommand ClearManifestSelectionCommand { get; }
        public ICommand ClearServiceSelectionCommand { get; }
        public string PlaceholderText { get; set; }

        public Brush HyperlinkBrush { get { return (Brush)GetValue(_hyperlinkBrushProperty); } set { SetValue(_hyperlinkBrushProperty, value); } }

        public static readonly DependencyProperty _hyperlinkBrushProperty = DependencyProperty.Register("HyperlinkBrush", typeof(Brush), typeof(DeploymentDialogView), new PropertyMetadata(default(Brush)));

        public DeploymentDialogView()
        {
            InitializeComponent();
        }

        public DeploymentDialogView(IDeploymentDialogViewModel viewModel, IThemeService themeService)
        {
            _viewModel = viewModel;
            bool alwaysTrue(object arg) { return true; };

            UploadAppCommand = new DelegatingCommand(viewModel.DeployApp, viewModel.CanDeployApp);
            OpenLoginDialogCommand = new DelegatingCommand(viewModel.OpenLoginView, viewModel.CanOpenLoginView);
            ToggleAdvancedOptionsCommand = new DelegatingCommand(viewModel.ToggleAdvancedOptions, viewModel.CanToggleAdvancedOptions);
            ClearServiceSelectionCommand = new DelegatingCommand(viewModel.ClearSelectedServices, alwaysTrue);
            ClearBuildpackSelectionCommand = new DelegatingCommand(viewModel.ClearSelectedBuildpacks, alwaysTrue);
            ClearManifestSelectionCommand = new DelegatingCommand(viewModel.ClearSelectedManifest, alwaysTrue);

            themeService.SetTheme(this);
            DataContext = viewModel;
            InitializeComponent();

            MouseDown += Window_MouseDown;
        }

        private void CfOrgOptions_ComboBox_DropDownClosed(object sender, System.EventArgs e)
        {
            var _ = _viewModel.UpdateCfSpaceOptions();
        }

        private void SelectManifest(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog
            {
                InitialDirectory = _viewModel.PathToProjectRootDir,
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
            var openFolderDialog = new FolderBrowserDialog
            {
                SelectedPath = _viewModel.PathToProjectRootDir,
            };

            if (openFolderDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                _viewModel.DeploymentDirectoryPath = openFolderDialog.SelectedPath;
            }
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

        private void BuildpackItemSelected(object sender, RoutedEventArgs e)
        {
            if (e.Source is System.Windows.Controls.CheckBox cb)
            {
                _viewModel.AddToSelectedBuildpacks(cb.Content);
            }
        }

        private void BuildpackItemDeselected(object sender, RoutedEventArgs e)
        {
            if (e.Source is System.Windows.Controls.CheckBox cb)
            {
                _viewModel.RemoveFromSelectedBuildpacks(cb.Content);
            }
        }

        private void ServiceItemSelected(object sender, RoutedEventArgs e)
        {
            if (e.Source is System.Windows.Controls.CheckBox cb)
            {
                _viewModel.AddToSelectedServices(cb.Content);
            }
        }

        private void ServiceItemDeselected(object sender, RoutedEventArgs e)
        {
            if (e.Source is System.Windows.Controls.CheckBox cb)
            {
                _viewModel.RemoveFromSelectedServices(cb.Content);
            }
        }

        private void SaveManifestButton_Click(object sender, RoutedEventArgs e)
        {
            var saveFileDialog = new SaveFileDialog()
            {
                DefaultExt = "yml",
                InitialDirectory = _viewModel.PathToProjectRootDir,
            };

            if (saveFileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                _viewModel.WriteManifestToFile(saveFileDialog.FileName);
            }
        }
    }
}
