using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Tanzu.Toolkit.ViewModels;
using Tanzu.Toolkit.WpfViews.Commands;
using Tanzu.Toolkit.WpfViews.Services;
using Tanzu.Toolkit.WpfViews.ThemeService;

namespace Tanzu.Toolkit.WpfViews
{
    /// <summary>
    /// Interaction logic for TasExplorerView.xaml.
    /// </summary>
    public partial class TasExplorerView : UserControl, ITasExplorerView, IView
    {
        private IViewService ViewService;

        public ICommand OpenLoginFormCommand { get; }
        public ICommand StopCfAppCommand { get; }
        public ICommand StartCfAppCommand { get; }
        public ICommand DeleteCfAppCommand { get; }
        public ICommand DisplayRecentAppLogsCommand { get; }
        public ICommand RefreshSpaceCommand { get; }
        public ICommand RefreshAllCommand { get; }
        public ICommand DeleteConnectionCommand { get; }
        public ICommand ReAuthenticateCommand { get; }
        public IViewModel ViewModel { get; private set; }

        public Brush ListItemMouseOverBrush { get { return (Brush)GetValue(ListItemMouseOverBrushProperty); } set { SetValue(ListItemMouseOverBrushProperty, value); } }
        public Brush WizardFooterBrush { get { return (Brush)GetValue(WizardFooterBrushProperty); } set { SetValue(WizardFooterBrushProperty, value); } }

        public static readonly DependencyProperty ListItemMouseOverBrushProperty = DependencyProperty.Register("ListItemMouseOverBrush", typeof(Brush), typeof(TasExplorerView), new PropertyMetadata(default(Brush)));
        public static readonly DependencyProperty WizardFooterBrushProperty = DependencyProperty.Register("WizardFooterBrush", typeof(Brush), typeof(TasExplorerView), new PropertyMetadata(default(Brush)));

        public TasExplorerView()
        {
            InitializeComponent();
        }

        public TasExplorerView(ITasExplorerViewModel viewModel, IThemeService themeService, IViewService viewService)
        {
            ViewModel = viewModel;
            ViewService = viewService;

            OpenLoginFormCommand = new DelegatingCommand(viewModel.OpenLoginView, viewModel.CanOpenLoginView);
            StopCfAppCommand = new AsyncDelegatingCommand(viewModel.StopCfApp, viewModel.CanStopCfApp);
            StartCfAppCommand = new AsyncDelegatingCommand(viewModel.StartCfApp, viewModel.CanStartCfApp);
            DeleteCfAppCommand = new AsyncDelegatingCommand(viewModel.DeleteCfApp, viewModel.CanDeleteCfApp);
            DisplayRecentAppLogsCommand = new AsyncDelegatingCommand(viewModel.DisplayRecentAppLogs, viewModel.CanDisplayRecentAppLogs);
            RefreshSpaceCommand = new DelegatingCommand(viewModel.RefreshSpace, viewModel.CanRefreshSpace);
            RefreshAllCommand = new DelegatingCommand(viewModel.RefreshAllItems, viewModel.CanInitiateFullRefresh);
            DeleteConnectionCommand = new DelegatingCommand(viewModel.DeleteConnection, viewModel.CanRemoveCloudConnecion);
            ReAuthenticateCommand = new DelegatingCommand(viewModel.ReAuthenticate, viewModel.CanReAuthenticate);

            themeService.SetTheme(this);

            DataContext = viewModel;
            InitializeComponent();
        }

        public void Show()
        {
            ViewService.DisplayViewByType(GetType());
        }
    }
}
