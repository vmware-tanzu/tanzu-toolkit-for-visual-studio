using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;
using Tanzu.Toolkit.Models;
using Tanzu.Toolkit.Services;
using Tanzu.Toolkit.Services.ErrorDialog;

namespace Tanzu.Toolkit.ViewModels.AppDeletionConfirmation
{
    public class AppDeletionConfirmationViewModel : AbstractViewModel, IAppDeletionConfirmationViewModel
    {
        internal const string _deleteAppErrorMsg = "Encountered an error while deleting app";
        private bool _deleteRoutes;
        internal CloudFoundryApp CfApp { get; set; }

        private readonly IErrorDialog _errorDialogService;

        public AppDeletionConfirmationViewModel(IServiceProvider services) : base(services)
        {
            _errorDialogService = services.GetRequiredService<IErrorDialog>();
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

        public void ShowConfirmation(CloudFoundryApp app)
        {
            CfApp = app;
            DialogService.ShowDialog(nameof(AppDeletionConfirmationViewModel));
            CfApp = null;
        }

        public async Task DeleteApp(object window = null)
        {
            try
            {
                var deleteResult = await CloudFoundryService.DeleteAppAsync(CfApp, removeRoutes: DeleteRoutes);
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