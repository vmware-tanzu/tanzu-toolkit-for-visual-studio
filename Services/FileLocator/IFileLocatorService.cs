namespace Tanzu.Toolkit.VisualStudio.Services.FileLocator
{
    public interface IFileLocatorService
    {
        string FullPathToCfExe { get; }
        string VsixPackageBaseDir { get; }

        bool DirContainsFiles(string dirPath);
    }
}