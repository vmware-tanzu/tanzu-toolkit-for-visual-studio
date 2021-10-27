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
        bool DirectoryExists(string dirPath);
        bool FileExists(string filePath);
        string ReadFileContents(string filePath);
        string[] ReadFileLines(string filePath);
        void WriteTextToFile(string filePath, string contentsToWrite);
    }
}