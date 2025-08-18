using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Tanzu.Toolkit.ViewModels;
using Tanzu.Toolkit.VisualStudio.Services;
using Tanzu.Toolkit.VisualStudio.Views.Commands;

namespace Tanzu.Toolkit.VisualStudio.Views
{
    /// <summary>
    /// Interaction logic for TanzuExplorerView.xaml.
    /// </summary>
    public partial class TanzuExplorerView : UserControl, ITanzuExplorerView, IView
    {
        public ICommand OpenLoginFormCommand { get; }
        public ICommand StopCfAppCommand { get; }
        public ICommand StartCfAppCommand { get; }
        public ICommand OpenDeletionViewCommand { get; }
        public ICommand RefreshSpaceCommand { get; }
        public ICommand RefreshOrgCommand { get; }
        public ICommand RefreshAllCommand { get; }
        public ICommand DeleteConnectionCommand { get; }
        public ICommand ReAuthenticateCommand { get; }
        public ICommand StreamAppLogsCommand { get; }
        public IViewModel ViewModel { get; private set; }

        public Brush ListItemMouseOverBrush
        {
            get => (Brush)GetValue(_listItemMouseOverBrushProperty);
            set => SetValue(_listItemMouseOverBrushProperty, value);
        }

        public Brush WizardFooterBrush
        {
            get => (Brush)GetValue(_wizardFooterBrushProperty);
            set => SetValue(_wizardFooterBrushProperty, value);
        }

        public Action DisplayView
        {
            get => throw new NotImplementedException();
            set => throw new NotImplementedException();
        }

        public static readonly DependencyProperty _listItemMouseOverBrushProperty =
            DependencyProperty.Register(nameof(ListItemMouseOverBrush), typeof(Brush), typeof(TanzuExplorerView),
                new PropertyMetadata(default(Brush)));

        public static readonly DependencyProperty _wizardFooterBrushProperty =
            DependencyProperty.Register(nameof(WizardFooterBrush), typeof(Brush), typeof(TanzuExplorerView),
                new PropertyMetadata(default(Brush)));

        public TanzuExplorerView()
        {
            InitializeComponent();
        }

        public TanzuExplorerView(ITanzuExplorerViewModel viewModel, IThemeService themeService)
        {
            ViewModel = viewModel;

            OpenLoginFormCommand = new AsyncDelegatingCommand(viewModel.OpenLoginViewAsync, viewModel.CanOpenLoginView);
            StopCfAppCommand = new AsyncDelegatingCommand(viewModel.StopCloudFoundryAppAsync, viewModel.CanStopCloudFoundryApp);
            StartCfAppCommand = new AsyncDelegatingCommand(viewModel.StartCloudFoundryAppAsync, viewModel.CanStartCloudFoundryApp);
            OpenDeletionViewCommand = new AsyncDelegatingCommand(viewModel.OpenDeletionViewAsync, viewModel.CanOpenDeletionView);
            RefreshSpaceCommand = new AsyncDelegatingCommand(viewModel.RefreshSpaceAsync, viewModel.CanRefreshSpace);
            RefreshOrgCommand = new AsyncDelegatingCommand(viewModel.RefreshOrgAsync, viewModel.CanRefreshOrg);
            RefreshAllCommand = new DelegatingCommand(viewModel.BackgroundRefreshAllItems, viewModel.CanInitiateFullRefresh);
            DeleteConnectionCommand = new DelegatingCommand(viewModel.LogOutCloudFoundry, viewModel.CanLogOutCloudFoundry);
            ReAuthenticateCommand = new AsyncDelegatingCommand(viewModel.ReAuthenticateAsync, viewModel.CanReAuthenticate);
            StreamAppLogsCommand = new AsyncDelegatingCommand(viewModel.StreamAppLogsAsync, arg => true);

            themeService.SetTheme(this);

            DataContext = viewModel;
            InitializeComponent();
        }

        private void Disconnect(object sender, RoutedEventArgs e)
        {
            DeleteConnectionCommand.Execute(null);
        }
    }
}