using System;
using System.IO;

namespace Tanzu.Toolkit.Services.File
{
    public class FileService : IFileService
    {
        private const string _cfCliV8ExeName = "cf8.exe";
        private const string _cfCliV8Dir = "Resources";
        private const string _defaultLogsFileName = "toolkit-diagnostics.log";
        private const string _defaultLogsDir = "Logs";
        private const string _defaultCfCliDir = ".cf";
        private const string _defaultCfCliConfigFileName = "config.json";
        private const string _cfDebugAdapterName = "CfSshWrapper.exe";
        private int _cliVersion = 8;
        private readonly string _pathToCf8Exe;

        public FileService(string vsixBaseDirPath)
        {
            VsixPackageBaseDir = vsixBaseDirPath;

            _pathToCf8Exe = Path.Combine(VsixPackageBaseDir, _cfCliV8Dir, _cfCliV8ExeName);
        }

        public int CliVersion
        {
            get => _cliVersion;
            set
            {
                if (value == 8)
                {
                    _cliVersion = 8;
                }
            }
        }

        public string FullPathToCfExe
        {
            get
            {
                if (_cliVersion != 8) throw new Exception($"Unable to locate cf.exe for CLI version {_cliVersion}.");

                return System.IO.File.Exists(_pathToCf8Exe)
                    ? _pathToCf8Exe
                    : throw new Exception($"Unable to locate cf.exe for CLI version {_cliVersion}.");
            }
        }

        public bool DirectoryExists(string dirPath)
        {
            return Directory.Exists(dirPath);
        }

        public bool DirContainsFiles(string dirPath)
        {
            return Directory.Exists(dirPath) && Directory.GetFiles(dirPath).Length > 0;
        }

        public string VsixPackageBaseDir { get; }

        public string PathToLogsFile => Path.Combine(VsixPackageBaseDir, _defaultLogsDir, _defaultLogsFileName);

        public string PathToCfCliConfigFile => Path.Combine(VsixPackageBaseDir, _defaultCfCliDir, _defaultCfCliConfigFileName);

        public string PathToCfDebugAdapter => Path.Combine(VsixPackageBaseDir, _cfDebugAdapterName);

        public void WriteTextToFile(string filePath, string contentsToWrite)
        {
            System.IO.File.WriteAllText(filePath, contentsToWrite);
        }

        public string ReadFileContents(string filePath)
        {
            return System.IO.File.ReadAllText(filePath);
        }

        public string[] ReadFileLines(string filePath)
        {
            return System.IO.File.ReadAllLines(filePath);
        }

        public bool FileExists(string filePath)
        {
            return System.IO.File.Exists(filePath);
        }

        public string GetUniquePathForTempFile(string fileName = "")
        {
            var tmpDirPath = Path.Combine(VsixPackageBaseDir, "tmp");

            if (!Directory.Exists(tmpDirPath))
            {
                Directory.CreateDirectory(tmpDirPath);
            }

            var uniqueFileName = fileName + DateTimeOffset.Now.ToUnixTimeSeconds().ToString();
            return Path.Combine(tmpDirPath, uniqueFileName);
        }

        public void DeleteFile(string filePath)
        {
            System.IO.File.Delete(filePath);
        }
    }
}