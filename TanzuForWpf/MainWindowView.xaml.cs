using System.Windows;
using System.Windows.Input;
using TanzuForVS.WpfViews.Commands;

namespace TanzuForWpf
{
    /// <summary>
    /// Interaction logic for MainWindowView.xaml
    /// </summary>
    public partial class MainWindowView : Window, IMainWindowView
    {
        public MainWindowView()
        {
            InitializeComponent();
        }

        public MainWindowView(IMainWindowViewModel viewModel)
        {
            OpenCloudExplorerCommand = new DelegatingCommand(viewModel.OpenCloudExplorer, viewModel.CanOpenCloudExplorer);
            DataContext = viewModel;
            InitializeComponent();
        }

        public ICommand OpenCloudExplorerCommand { get; }
    }
}
