namespace Tanzu.Toolkit.Services.Project
{
    public interface IProjectService
    {
        string ProjectName { get; set; }
        string PathToProjectDirectory { get; set; }
        string TargetFrameworkMoniker { get; set; }
    }
}