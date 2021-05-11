using System;
using System.Windows;
using System.Windows.Input;
using Tanzu.Toolkit.VisualStudio.ViewModels;
using Tanzu.Toolkit.VisualStudio.WpfViews.Commands;

namespace Tanzu.Toolkit.VisualStudio.WpfViews
{
    /// <summary>
    /// Interaction logic for ErrorDialogView.xaml
    /// </summary>
    public partial class ErrorDialogView : Window, IErrorDialogView
    {
        private IErrorDialogViewModel _viewModel;
        public ICommand CloseCommand { get; }

        public ErrorDialogView()
        {
            InitializeComponent();
        }

        public ErrorDialogView(IErrorDialogViewModel viewModel)
        {
            _viewModel = viewModel;
            CloseCommand = new DelegatingCommand(CloseCommandHandler, viewModel.CanClose);
            DataContext = viewModel;
            InitializeComponent();

        }
        public void CloseCommandHandler(object sender)
        {
            Close();
        }

    }
    

}
