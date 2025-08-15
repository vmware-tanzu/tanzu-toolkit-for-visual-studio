using System.Windows.Input;
using Tanzu.Toolkit.ViewModels.AppDeletionConfirmation;
using Tanzu.Toolkit.VisualStudio.Views.Commands;

namespace Tanzu.Toolkit.VisualStudio.Views
{
    /// <summary>
    /// Interaction logic for ConfirmationDeleteView.xaml
    /// </summary>
    public partial class AppDeletionConfirmationView : AbstractModal, IAppDeletionConfirmationView
    {
        public ICommand DeleteAppCommand { get; }

        public AppDeletionConfirmationView(IAppDeletionConfirmationViewModel viewModel)
        {
            DeleteAppCommand = new AsyncDelegatingCommand(viewModel.DeleteApp, viewModel.CanDeleteApp);
            DataContext = viewModel;
            MouseDown += Window_MouseDown;
            InitializeComponent();
        }

        private void Close(object sender, System.Windows.RoutedEventArgs e)
        {
            Close();
        }
    }
}