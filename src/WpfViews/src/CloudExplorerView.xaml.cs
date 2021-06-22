using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Tanzu.Toolkit.Services.ThemeService;
using Tanzu.Toolkit.ViewModels;
using Tanzu.Toolkit.WpfViews.Commands;

namespace Tanzu.Toolkit.WpfViews
{
    /// <summary>
    /// Interaction logic for CloudExplorerView.xaml.
    /// </summary>
    public partial class CloudExplorerView : UserControl, ICloudExplorerView
    {
        public ICommand OpenLoginFormCommand { get; }
        public ICommand StopCfAppCommand { get; }
        public ICommand StartCfAppCommand { get; }
        public ICommand DeleteCfAppCommand { get; }
        public ICommand DisplayRecentAppLogsCommand { get; }
        public ICommand RefreshSpaceCommand { get; }
        public ICommand RefreshAllCommand { get; }
        public ICommand RemoveCloudConnectionCommand { get; }
        public Color Color { get; private set; }



        public CloudExplorerView()
        {
            InitializeComponent();
        }

        public CloudExplorerView(ICloudExplorerViewModel viewModel, IThemeService brush)
        {
            OpenLoginFormCommand = new DelegatingCommand(viewModel.OpenLoginView, viewModel.CanOpenLoginView);
            StopCfAppCommand = new AsyncDelegatingCommand(viewModel.StopCfApp, viewModel.CanStopCfApp);
            StartCfAppCommand = new AsyncDelegatingCommand(viewModel.StartCfApp, viewModel.CanStartCfApp);
            DeleteCfAppCommand = new AsyncDelegatingCommand(viewModel.DeleteCfApp, viewModel.CanDeleteCfApp);
            DisplayRecentAppLogsCommand = new AsyncDelegatingCommand(viewModel.DisplayRecentAppLogs, viewModel.CanDisplayRecentAppLogs);
            RefreshSpaceCommand = new DelegatingCommand(viewModel.RefreshSpace, viewModel.CanRefreshSpace);
            RefreshAllCommand = new DelegatingCommand(viewModel.RefreshAllItems, viewModel.CanInitiateFullRefresh);
            RemoveCloudConnectionCommand = new DelegatingCommand(viewModel.RemoveCloudConnection, viewModel.CanRemoveCloudConnecion);

            //var practiceColor = brush.practiceBrush;
            //Color = (Color)ColorConverter.ConvertFromString();
            var colorUint = brush.MyColor;

            // TODO convert from uint to WPF Color (saystem.windows.media.color?)
            DataContext = viewModel;
            InitializeComponent();
        }

        

        public Brush MyCoolBrush
        {
            get { return new SolidColorBrush(Color); }
        }

    }
}
