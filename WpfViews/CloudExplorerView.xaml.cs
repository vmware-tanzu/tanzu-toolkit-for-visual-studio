using System.Windows.Controls;
using System.Windows.Input;
using TanzuForVS.ViewModels;
using TanzuForVS.WpfViews.Commands;

namespace TanzuForVS.WpfViews
{
    /// <summary>
    /// Interaction logic for CloudExplorerView.xaml
    /// </summary>
    public partial class CloudExplorerView : UserControl, ICloudExplorerView
    {
        public CloudExplorerView()
        {
            InitializeComponent();
        }

        public CloudExplorerView(ICloudExplorerViewModel viewModel)
        {
            OpenLoginFormCommand = new DelegatingCommand(viewModel.OpenLoginView, viewModel.CanOpenLoginView);
            DataContext = viewModel;
            InitializeComponent();
        }

        public ICommand OpenLoginFormCommand { get; }
    }
}
