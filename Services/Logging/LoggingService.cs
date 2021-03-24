using Serilog;

namespace Tanzu.Toolkit.VisualStudio.Services.Logging
{
    public class LoggingService : ILoggingService
    {
        public ILogger Logger { get; }

        public LoggingService()
        {
            Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.File(
                    path: "toolkit-diagnostics.log", 
                    shared: true // allow multiple processes to share same log file
                ).CreateLogger();
        }
    }
}
