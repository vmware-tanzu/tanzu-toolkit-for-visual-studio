using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;
using Tanzu.Toolkit.Models;
using Tanzu.Toolkit.Services.ErrorDialog;

namespace Tanzu.Toolkit.ViewModels.AppDeletionConfirmation
{
    public class AppDeletionConfirmationViewModel : AbstractViewModel, IAppDeletionConfirmationViewModel
    {
        internal const string _deleteAppErrorMsg = "Encountered an error while deleting app";
        private string _message;
        private bool _deleteRoutes;
        internal CloudFoundryApp CfApp { get; set; }

        private readonly IErrorDialog _errorDialogService;
        private readonly ITanzuExplorerViewModel _tanzuExplorer;

        public AppDeletionConfirmationViewModel(ITanzuExplorerViewModel tanzuExplorerViewModel, IServiceProvider services) : base(services)
        {
            _errorDialogService = services.GetRequiredService<IErrorDialog>();
            _tanzuExplorer = tanzuExplorerViewModel;
            var identifier = CfApp == null || CfApp.AppName == null || string.IsNullOrWhiteSpace(CfApp.AppName)
                ? "this app"
                : $"\"CfApp.AppName\"";
            Message = $"Are you sure you want to delete {identifier}?";
        }

        public bool CanDeleteApp(object arg)
        {
            return true;
        }

        public bool DeleteRoutes
        {
            get { return _deleteRoutes; }
            set
            {
                _deleteRoutes = value;
                RaisePropertyChangedEvent("DeleteRoutes");
            }
        }

        public string Message
        {
            get { return _message; }
            set
            {
                _message = value;
                RaisePropertyChangedEvent("Message");
            }
        }

        public void ShowConfirmation(CloudFoundryApp app)
        {
            CfApp = app;
            var dialog = DialogService.ShowModal(nameof(AppDeletionConfirmationViewModel));
            if (dialog == null)
            {
                Logger?.Error("{ClassName}.{MethodName} encountered null DialogResult, indicating that something went wrong trying to construct the view.", nameof(AppDeletionConfirmation), nameof(ShowConfirmation));
                var title = "Something went wrong while trying to display app deletion confirmation";
                var msg = "View construction failed"+
                    Environment.NewLine + Environment.NewLine +
                    "If this issue persists, please contact tas-vs-extension@vmware.com";
                ErrorService.DisplayErrorDialog(title, msg);
            }
            CfApp = null;
        }

        public async Task DeleteApp(object window = null)
        {
            try
            {
                var deleteResult = await _tanzuExplorer.CloudFoundryConnection.CfClient.DeleteAppAsync(CfApp, removeRoutes: DeleteRoutes);
                if (!deleteResult.Succeeded)
                {
                    Logger.Error(_deleteAppErrorMsg + " {AppName}. {DeleteResult}", CfApp.AppName, deleteResult.ToString());
                    _errorDialogService.DisplayErrorDialog($"{_deleteAppErrorMsg} {CfApp.AppName}.", deleteResult.Explanation);
                }
            }
            catch (Exception ex)
            {
                Logger.Error(_deleteAppErrorMsg + " {AppName}. {AppDeletionException}", CfApp.AppName, ex.Message);
                _errorDialogService.DisplayWarningDialog($"{_deleteAppErrorMsg} {CfApp.AppName}.", $"Something unexpected happened while deleting {CfApp.AppName}");
            }

            CfApp = null;
            DialogService.CloseDialog(window, true);
        }
    }
}