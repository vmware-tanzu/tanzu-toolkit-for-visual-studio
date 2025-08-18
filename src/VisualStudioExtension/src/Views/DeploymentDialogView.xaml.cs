using System;
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
    public partial class DeploymentDialogView : AbstractModal, IDeploymentDialogView
    {
        public ICommand UploadAppCommand { get; }
        public ICommand OpenLoginDialogCommand { get; }
        public ICommand ToggleAdvancedOptionsCommand { get; }
        public ICommand ClearBuildpackSelectionCommand { get; }
        public DelegatingCommand ClearManifestSelectionCommand { get; }
        public DelegatingCommand ClearDeploymentDirectorySelectionCommand { get; }
        public ICommand ClearServiceSelectionCommand { get; }

        public Brush HyperlinkBrush
        {
            get => (Brush)GetValue(_hyperlinkBrushProperty);
            set => SetValue(_hyperlinkBrushProperty, value);
        }

        public static readonly DependencyProperty _hyperlinkBrushProperty =
            DependencyProperty.Register(nameof(HyperlinkBrush), typeof(Brush), typeof(DeploymentDialogView),
                new PropertyMetadata(default(Brush)));

        private readonly IDeploymentDialogViewModel _viewModel;

        public DeploymentDialogView(IDeploymentDialogViewModel viewModel, IThemeService themeService)
        {
            _viewModel = viewModel;

            UploadAppCommand = new DelegatingCommand(viewModel.DeployApp, viewModel.CanDeployApp);
            OpenLoginDialogCommand = new AsyncDelegatingCommand(viewModel.OpenLoginViewAsync, viewModel.CanOpenLoginView);
            ToggleAdvancedOptionsCommand =
                new DelegatingCommand(viewModel.ToggleAdvancedOptions, viewModel.CanToggleAdvancedOptions);
            ClearServiceSelectionCommand = new DelegatingCommand(viewModel.ClearSelectedServices, AlwaysTrue);
            ClearBuildpackSelectionCommand = new DelegatingCommand(viewModel.ClearSelectedBuildpacks, AlwaysTrue);
            ClearManifestSelectionCommand = new DelegatingCommand(viewModel.ClearSelectedManifest, AlwaysTrue);
            ClearDeploymentDirectorySelectionCommand =
                new DelegatingCommand(viewModel.ClearSelectedDeploymentDirectory, AlwaysTrue);

            themeService.SetTheme(this);
            DataContext = viewModel;
            InitializeComponent();

            MouseDown += Window_MouseDown;
            return;

            bool AlwaysTrue(object arg) => true;
        }

        protected override void OnContentRendered(EventArgs e)
        {
            _viewModel.OnRendered();
            base.OnContentRendered(e);
        }

        private void CfOrgOptions_ComboBox_DropDownClosed(object sender, System.EventArgs e)
        {
            _ = _viewModel.UpdateCfSpaceOptionsAsync();
        }

        private void SelectManifest(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog
            {
                InitialDirectory = _viewModel.PathToProjectRootDir, Filter = "YAML files (*.yaml, *.yml)|*.yaml;*.yml", FilterIndex = 2, RestoreDirectory = true
            };

            if (openFileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                _viewModel.ManifestPath = openFileDialog.FileName;
            }
        }

        private void SelectPublishDirectory(object sender, RoutedEventArgs e)
        {
            var openFolderDialog = new FolderBrowserDialog { SelectedPath = _viewModel.PathToProjectRootDir };

            if (openFolderDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                _viewModel.DeploymentDirectoryPath = openFolderDialog.SelectedPath;
            }
        }

        private void Close(object sender, RoutedEventArgs e)
        {
            Hide(); // important to hide instead of closing (which has a side effect of permanently closing LoginView)
            _viewModel.OnClose();
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
            var saveFileDialog = new SaveFileDialog { DefaultExt = "yml", InitialDirectory = _viewModel.PathToProjectRootDir };

            if (saveFileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                _viewModel.WriteManifestToFile(saveFileDialog.FileName);
            }
        }
    }
}