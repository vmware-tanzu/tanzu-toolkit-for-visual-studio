using Serilog;
using Tanzu.Toolkit.VisualStudio.Services.FileLocator;

namespace Tanzu.Toolkit.VisualStudio.Services.Logging
{
    public class LoggingService : ILoggingService
    {
        public ILogger Logger { get; }

        public LoggingService(IFileLocatorService fileLocatorService)
        {
            Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.File(
                    path: fileLocatorService.PathToLogsFile,
                    shared: true, // allow multiple processes to share same log file
                    fileSizeLimitBytes: 32768, // 32 KiB
                    rollOnFileSizeLimit: true,
                    retainedFileCountLimit: 8
                ).CreateLogger();

            Logger.Information("Logging Service Initialized");
        }
    }
}
