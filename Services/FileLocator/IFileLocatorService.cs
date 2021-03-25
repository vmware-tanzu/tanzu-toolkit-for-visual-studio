namespace Tanzu.Toolkit.VisualStudio.Services.FileLocator
{
    public interface IFileLocatorService
    {
        string FullPathToCfExe { get; }
        string VsixPackageBaseDir { get; }
        string PathToLogsFile { get; }

        bool DirContainsFiles(string dirPath);
    }
}