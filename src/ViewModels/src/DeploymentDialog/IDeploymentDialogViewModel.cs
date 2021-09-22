using System.Threading.Tasks;

namespace Tanzu.Toolkit.ViewModels
{
    public interface IDeploymentDialogViewModel
    {
        string ProjectDirPath { get; }
        string ManifestPath { get; set; }
        string DirectoryPath { get; set; }

        bool CanDeployApp(object arg);
        bool CanOpenLoginView(object arg);
        void DeployApp(object arg);
        void OpenLoginView(object arg);
        Task UpdateCfOrgOptions();
        Task UpdateCfSpaceOptions();
    }
}