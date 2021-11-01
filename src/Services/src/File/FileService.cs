using System;
using System.IO;

namespace Tanzu.Toolkit.Services.File
{
    public class FileService : IFileService
    {
        private const string _cfCliV6ExeName = "cf6.exe";
        private const string _cfCliV6Dir = "Resources";
        private const string _cfCliV7ExeName = "cf7.exe";
        private const string _cfCliV7Dir = "Resources";
        private const string _defaultLogsFileName = "toolkit-diagnostics.log";
        private const string _defaultLogsDir = "Logs";
        private const string _defaultCfCliDir = ".cf";
        private const string _defaultCfCliConfigFileName = "config.json";
        private int _cliVersion = 7;
        private readonly string _pathToCf6Exe;
        private readonly string _pathToCf7Exe;
        private readonly string _vsixBaseDirPath;

        public FileService(string vsixBaseDirPath)
        {
            _vsixBaseDirPath = vsixBaseDirPath;

            _pathToCf7Exe = Path.Combine(VsixPackageBaseDir, _cfCliV7Dir, _cfCliV7ExeName);
            _pathToCf6Exe = Path.Combine(VsixPackageBaseDir, _cfCliV6Dir, _cfCliV6ExeName);
        }

        public int CliVersion
        {
            get => _cliVersion;
            set
            {
                switch (value)
                {
                    case 6:
                        _cliVersion = 6;
                        break;
                    case 7:
                        _cliVersion = 7;
                        break;
                    default:
                        break;
                }
            }
        }

        public string FullPathToCfExe
        {
            get
            {
                switch (_cliVersion)
                {
                    case 6:
                        if (System.IO.File.Exists(_pathToCf6Exe))
                        {
                            return _pathToCf6Exe;
                        }

                        break;
                    case 7:
                        if (System.IO.File.Exists(_pathToCf7Exe))
                        {
                            return _pathToCf7Exe;
                        }

                        break;
                }

                throw new Exception($"Unable to locate cf.exe for CLI version {_cliVersion}.");
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

        public string VsixPackageBaseDir
        {
            get
            {
                return _vsixBaseDirPath;
            }
        }

        public string PathToLogsFile
        {
            get
            {
                return Path.Combine(VsixPackageBaseDir, _defaultLogsDir, _defaultLogsFileName);
            }
        }

        public string PathToCfCliConfigFile
        {
            get
            {
                return Path.Combine(VsixPackageBaseDir, _defaultCfCliDir, _defaultCfCliConfigFileName);
            }
        }

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
            string tmpDirPath = Path.Combine(VsixPackageBaseDir, "tmp");

            if (!Directory.Exists(tmpDirPath))
            {
                Directory.CreateDirectory(tmpDirPath);
            }

            string uniqueFileName = fileName + DateTimeOffset.Now.ToUnixTimeSeconds().ToString();
            return Path.Combine(tmpDirPath, uniqueFileName);
        }

        public void DeleteFile(string filePath)
        {
            System.IO.File.Delete(filePath);
        }
    }
}
