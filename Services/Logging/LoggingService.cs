using Serilog;
using System.IO;
using Tanzu.Toolkit.VisualStudio.Services.FileLocator;

namespace Tanzu.Toolkit.VisualStudio.Services.Logging
{
    public class LoggingService : ILoggingService
    {
        public ILogger Logger { get; }

        public LoggingService(IFileLocatorService fileLocatorService)
        {
            var logFileName = "Logs/toolkit-diagnostics.log";
            var logFilePath = Path.Combine(fileLocatorService.VsixPackageBaseDir, logFileName);

            Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.File(
                    path: logFilePath, 
                    shared: true // allow multiple processes to share same log file
                ).CreateLogger();
        }
    }
}
