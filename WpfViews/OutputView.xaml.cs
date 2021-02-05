using System.Windows.Controls;
using Tanzu.Toolkit.VisualStudio.ViewModels;

namespace Tanzu.Toolkit.VisualStudio.WpfViews
{
    /// <summary>
    /// Interaction logic for OutputView.xaml
    /// </summary>
    public partial class OutputView : UserControl, IOutputView, IView
    {
        public IViewModel ViewModel { get; private set; }

        public OutputView()
        {
            InitializeComponent();
        }

        public OutputView(IOutputViewModel viewModel)
        {
            DataContext = viewModel;
            ViewModel = viewModel as IViewModel;
            InitializeComponent();
        }

    }
}
