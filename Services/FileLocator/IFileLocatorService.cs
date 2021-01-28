namespace TanzuForVS.Services.FileLocator
{
    public interface IFileLocatorService
    {
        string FullPathToCfExe { get; }

        bool DirContainsFiles(string dirPath);
    }
}