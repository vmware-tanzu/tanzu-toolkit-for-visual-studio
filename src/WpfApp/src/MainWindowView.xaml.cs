using System.Windows;
using System.Windows.Input;
using Tanzu.Toolkit.WpfViews.Commands;

namespace Tanzu.Toolkit.WpfApp
{
    /// <summary>
    /// Interaction logic for MainWindowView.xaml.
    /// </summary>
    public partial class MainWindowView : Window, IMainWindowView
    {
        public MainWindowView()
        {
            InitializeComponent();
        }

        public MainWindowView(IMainWindowViewModel viewModel)
        {
            OpenTasExplorerCommand = new DelegatingCommand(viewModel.OpenTasExplorer, viewModel.CanOpenTasExplorer);
            InvokeCommandPromptCommand = new DelegatingCommand(viewModel.InvokeCommandPrompt, viewModel.CanInvokeCommandPrompt);
            InvokeCfCliCommand = new DelegatingCommand(viewModel.InvokeCfCli, viewModel.CanInvokeCfCli);
            DataContext = viewModel;
            InitializeComponent();
        }

        public ICommand OpenTasExplorerCommand { get; }
        public ICommand InvokeCommandPromptCommand { get; }
        public ICommand InvokeCfCliCommand { get; }
    }
}
