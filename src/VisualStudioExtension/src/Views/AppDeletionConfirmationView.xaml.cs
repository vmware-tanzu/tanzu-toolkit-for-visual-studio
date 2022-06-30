using Microsoft.VisualStudio.PlatformUI;
using System;
using System.Windows.Input;
using Tanzu.Toolkit.ViewModels;
using Tanzu.Toolkit.ViewModels.AppDeletionConfirmation;
using Tanzu.Toolkit.VisualStudio.Views.Commands;

namespace Tanzu.Toolkit.VisualStudio.Views
{
    /// <summary>
    /// Interaction logic for ConfirmationDeleteView.xaml
    /// </summary>
    public partial class AppDeletionConfirmationView : DialogWindow, IAppDeletionConfirmationView, IView
    {
        public IAppDeletionConfirmationViewModel _confirmDeleteViewModel;
        private readonly IAppDeletionConfirmationViewModel _viewModel;

        public ICommand DeleteAppCommand { get; }

        public IViewModel ViewModel => (IViewModel)_viewModel;

        public Action DisplayView { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public AppDeletionConfirmationView(IAppDeletionConfirmationViewModel viewModel)
        {
            _viewModel = viewModel;
            DeleteAppCommand = new AsyncDelegatingCommand(viewModel.DeleteApp, viewModel.CanDeleteApp);
            DataContext = viewModel;
            _confirmDeleteViewModel = viewModel;

            MouseDown += Window_MouseDown;

            InitializeComponent();
        }

        private void Close(object sender, System.Windows.RoutedEventArgs e)
        {
            Close();
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                DragMove();
            }
        }
    }
}
