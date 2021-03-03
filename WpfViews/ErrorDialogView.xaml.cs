using System.Windows;
using Tanzu.Toolkit.VisualStudio.ViewModels;

namespace Tanzu.Toolkit.VisualStudio.WpfViews
{
    /// <summary>
    /// Interaction logic for ErrorDialogView.xaml
    /// </summary>
    public partial class ErrorDialogView : Window, IErrorDialogView
    {
        private IErrorDialogViewModel _viewModel;

        public ErrorDialogView()
        {
            InitializeComponent();
        }

        public ErrorDialogView(IErrorDialogViewModel viewModel)
        {
            _viewModel = viewModel;

            DataContext = viewModel;
            InitializeComponent();
        }
    }
}
