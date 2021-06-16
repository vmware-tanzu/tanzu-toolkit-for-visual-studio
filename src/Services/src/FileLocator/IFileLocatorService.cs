namespace Tanzu.Toolkit.Services.FileLocator
{
    public interface IFileLocatorService
    {
        string FullPathToCfExe { get; }
        string VsixPackageBaseDir { get; }
        string PathToLogsFile { get; }
        int CliVersion { get; set; }
        string PathToCfCliConfigFile { get; }

        bool DirContainsFiles(string dirPath);
    }
}