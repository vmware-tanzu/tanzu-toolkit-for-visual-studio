using System;
using System.Windows;
using Tanzu.Toolkit.ViewModels;
using System.Windows.Input;
using Tanzu.Toolkit.WpfViews.Commands;

namespace Tanzu.Toolkit.WpfViews
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
