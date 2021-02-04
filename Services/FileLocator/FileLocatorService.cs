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
                var pathToVsixAssemblies = Path.GetDirectoryName(GetType().Assembly.Location);
                var pathToCfExe = Path.Combine(pathToVsixAssemblies, _defaultCfCliDir, _defaultCfExeName);

                if (File.Exists(pathToCfExe)) return pathToCfExe;
                return null;
            }
        }

        public bool DirContainsFiles(string dirPath)
        {
            return Directory.Exists(dirPath) && Directory.GetFiles(dirPath).Length > 0;
        }
    }
}
