using System.Threading.Tasks;

namespace Tanzu.Toolkit.ViewModels
{
    public interface IDeploymentDialogViewModel
    {
        bool CanDeployApp(object arg);
        bool CanOpenLoginView(object arg);
        void DeployApp(object arg);
        void OpenLoginView(object arg);
        void UpdateCfInstanceOptions();
        Task UpdateCfOrgOptions();
        Task UpdateCfSpaceOptions();
    }
}