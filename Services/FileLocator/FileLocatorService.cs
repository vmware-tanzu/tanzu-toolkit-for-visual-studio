using System.IO;

namespace Tanzu.Toolkit.VisualStudio.Services.FileLocator
{
    public class FileLocatorService : IFileLocatorService
    {
        const string _cfCliV6ExeName = "cf6.exe";
        const string _cfCliV6Dir = "Resources";
        const string _cfCliV7ExeName = "cf7.exe";
        const string _cfCliV7Dir = "Resources";
        const string _defaultLogsFileName = "toolkit-diagnostics.log";
        const string _defaultLogsDir = "Logs";
        private int cliVersion = 7;
        private readonly string pathToCf6Exe;
        private readonly string pathToCf7Exe;
        private readonly string _vsixBaseDirPath;

        public FileLocatorService(string vsixBaseDirPath)
        {
            _vsixBaseDirPath = vsixBaseDirPath;

            pathToCf7Exe = Path.Combine(VsixPackageBaseDir, _cfCliV7Dir, _cfCliV7ExeName);
            pathToCf6Exe = Path.Combine(VsixPackageBaseDir, _cfCliV6Dir, _cfCliV6ExeName);
        }

        public int CliVersion
        {
            get => cliVersion;
            set
            {
                switch (value)
                {
                    case 6:
                        cliVersion = 6;
                        break;
                    case 7:
                        cliVersion = 7;
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
                switch (cliVersion)
                {
                    case 6:
                        if (File.Exists(pathToCf6Exe)) return pathToCf6Exe;
                        break;
                    case 7:
                        if (File.Exists(pathToCf7Exe)) return pathToCf7Exe;
                        break;
                }

                return null;
            }
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
    }
}
