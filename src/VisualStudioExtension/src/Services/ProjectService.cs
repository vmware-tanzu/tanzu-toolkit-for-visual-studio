using System;
using Tanzu.Toolkit.Services.Project;

namespace Tanzu.Toolkit.VisualStudio.Services
{
    public class ProjectService : IProjectService
    {
        private string _projectName;
        private string _pathToProjectDirectory;
        private string _targetFrameworkMoniker;

        public string ProjectName
        {
            get => string.IsNullOrEmpty(_projectName) ? throw new ArgumentException($"{nameof(ProjectName)} not set") : _projectName;

            set => _projectName = value;
        }

        public string PathToProjectDirectory
        {
            get =>
                string.IsNullOrEmpty(_pathToProjectDirectory)
                    ? throw new ArgumentException($"{nameof(PathToProjectDirectory)} not set")
                    : _pathToProjectDirectory;
            set => _pathToProjectDirectory = value;
        }

        public string TargetFrameworkMoniker
        {
            get =>
                string.IsNullOrEmpty(_targetFrameworkMoniker)
                    ? throw new ArgumentException($"{nameof(TargetFrameworkMoniker)} not set")
                    : _targetFrameworkMoniker;
            set => _targetFrameworkMoniker = value;
        }
    }
}