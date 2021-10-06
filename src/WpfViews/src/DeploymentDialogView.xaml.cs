using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
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
        public ICommand UploadAppCommand { get; }
        public ICommand OpenLoginDialogCommand { get; }
        public ICommand ToggleAdvancedOptionsCommand { get; }

        public Brush HyperlinkBrush { get { return (Brush)GetValue(HyperlinkBrushProperty); } set { SetValue(HyperlinkBrushProperty, value); } }

        public static readonly DependencyProperty HyperlinkBrushProperty = DependencyProperty.Register("HyperlinkBrush", typeof(Brush), typeof(DeploymentDialogView), new PropertyMetadata(default(Brush)));

        public DeploymentDialogView()
        {
            InitializeComponent();
        }

        public DeploymentDialogView(IDeploymentDialogViewModel viewModel, IThemeService themeService)
        {
            _viewModel = viewModel;
            UploadAppCommand = new DelegatingCommand(viewModel.DeployApp, viewModel.CanDeployApp);
            OpenLoginDialogCommand = new DelegatingCommand(viewModel.OpenLoginView, viewModel.CanOpenLoginView);
            ToggleAdvancedOptionsCommand = new DelegatingCommand(ToggleAdvancedOptions, viewModel.CanToggleAdvancedOptions);

            themeService.SetTheme(this);
            DataContext = viewModel;
            InitializeComponent();
            
            MouseDown += Window_MouseDown;
        }

        private void CfOrgOptions_ComboBox_DropDownClosed(object sender, System.EventArgs e)
        {
            _viewModel.UpdateCfSpaceOptions();
        }

        private void SelectManifest(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
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
            FolderBrowserDialog openFolderDialog = new FolderBrowserDialog
            {
                SelectedPath = _viewModel.PathToProjectRootDir,
            };

            if (openFolderDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                _viewModel.DeploymentDirectoryPath = openFolderDialog.SelectedPath;
            }
        }

        private void ToggleAdvancedOptions(object arg)
        {
            _viewModel.ToggleAdvancedOptions(arg);
            
            if (_viewModel.Expanded)
            {
                Height = (double)Resources["expandedWindowHeight"];
                GridBody.Height = (double)Resources["expandedGridBodyHeight"]; ;
            }
            else
            {
                Height = (double)Resources["collapsedWindowHeight"];
                GridBody.Height = (double)Resources["collapsedGridBodyHeight"]; ;
            }
        }

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