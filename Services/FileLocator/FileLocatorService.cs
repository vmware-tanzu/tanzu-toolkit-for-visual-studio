using System.IO;

namespace Tanzu.Toolkit.VisualStudio.Services.FileLocator
{
    public class FileLocatorService : IFileLocatorService
    {
        const string _defaultCfExeName = "cf.exe";
        const string _defaultCfCliDir = "Resources";

        public string FullPathToCfExe
        {
            get
            {
                var pathToCfExe = Path.Combine(VsixPackageBaseDir, _defaultCfCliDir, _defaultCfExeName);

                if (File.Exists(pathToCfExe)) return pathToCfExe;
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
                return Path.GetDirectoryName(GetType().Assembly.Location);
            }
        }
    }
}
