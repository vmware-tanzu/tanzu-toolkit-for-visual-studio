namespace Tanzu.Toolkit.VisualStudio.Services.FileLocator
{
    public interface IFileLocatorService
    {
        string FullPathToCfExe { get; }
        string VsixPackageBaseDir { get; }
        string PathToLogsFile { get; }
        int CliVersion { get; set; }

        bool DirContainsFiles(string dirPath);
    }
}