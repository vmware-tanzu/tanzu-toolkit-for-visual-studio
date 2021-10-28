﻿using System.Collections.Generic;
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
    }
}