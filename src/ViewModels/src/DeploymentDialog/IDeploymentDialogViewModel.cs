using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace Tanzu.Toolkit.ViewModels
{
    public interface IDeploymentDialogViewModel
    {
        string PathToProjectRootDir { get; }
        string ManifestPath { get; set; }
        string DeploymentDirectoryPath { get; set; }
        bool Expanded { get; set; }
        ObservableCollection<string> SelectedBuildpacks { get; set; }
        ObservableCollection<string> SelectedServices { get; set; }
        bool DeploymentInProgress { get; }
        IView OutputView { get; }
        bool ConfigureForRemoteDebugging { get; set; }

        bool CanDeployApp(object arg);
        bool CanToggleAdvancedOptions(object arg);
        bool CanOpenLoginView(object arg);
        void DeployApp(object arg);
        void OpenLoginView(object arg);
        void ToggleAdvancedOptions(object arg);
        Task UpdateCfOrgOptions();
        Task UpdateCfSpaceOptions();
        void AddToSelectedBuildpacks(object arg);
        void RemoveFromSelectedBuildpacks(object arg);
        void ClearSelectedBuildpacks(object arg = null);
        void WriteManifestToFile(string path);
        void AddToSelectedServices(object arg);
        void RemoveFromSelectedServices(object arg);
        void ClearSelectedServices(object arg = null);
        void ClearSelectedManifest(object arg = null);
    }
}