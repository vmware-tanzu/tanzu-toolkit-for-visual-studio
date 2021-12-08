﻿using Microsoft.VisualStudio.PlatformUI;
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
        private IDeploymentDialogViewModel _viewModel;
        public ICommand UploadAppCommand { get; }
        public ICommand OpenLoginDialogCommand { get; }
        public ICommand ToggleAdvancedOptionsCommand { get; }
        public ICommand ClearBuildpackSelectionCommand { get; }
        public string PlaceholderText { get; set; }

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
            ToggleAdvancedOptionsCommand = new DelegatingCommand(viewModel.ToggleAdvancedOptions, viewModel.CanToggleAdvancedOptions);
            ClearBuildpackSelectionCommand = new DelegatingCommand(viewModel.ClearSelectedBuildpacks, (object arg) => { return true; });

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

        private void CheckBox_Checked(object sender, RoutedEventArgs e)
        {
            if (e.Source is System.Windows.Controls.CheckBox cb)
            {
                _viewModel.AddToSelectedBuildpacks(cb.Content);
            }
        }

        private void CheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            if (e.Source is System.Windows.Controls.CheckBox cb)
            {
                _viewModel.RemoveFromSelectedBuildpacks(cb.Content);
            }
        }

        private void SaveManifestButton_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog()
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