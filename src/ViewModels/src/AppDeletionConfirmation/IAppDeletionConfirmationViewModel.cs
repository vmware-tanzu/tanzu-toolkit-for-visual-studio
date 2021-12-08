using System.Threading.Tasks;
using Tanzu.Toolkit.Models;

namespace Tanzu.Toolkit.ViewModels.AppDeletionConfirmation
{
    public interface IAppDeletionConfirmationViewModel
    {
        bool DeleteRoutes { get; set; }
        bool CanDeleteApp(object arg);
        Task DeleteApp(object arg);
        void ShowConfirmation(CloudFoundryApp app);
    }
}