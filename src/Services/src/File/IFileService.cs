namespace Tanzu.Toolkit.Services.File
{
    public interface IFileService
    {
        string FullPathToCfExe { get; }
        string VsixPackageBaseDir { get; }
        string PathToLogsFile { get; }
        int CliVersion { get; set; }
        string PathToCfCliConfigFile { get; }

        bool DirContainsFiles(string dirPath);
        string ReadFileContents(string filePath);
        void WriteTextToFile(string filePath, string contentsToWrite);
    }
}