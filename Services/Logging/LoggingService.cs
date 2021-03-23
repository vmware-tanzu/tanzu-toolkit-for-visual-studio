using Serilog;

namespace Tanzu.Toolkit.VisualStudio.Services.Logging
{
    public class LoggingService : ILoggingService
    {
        public ILogger Logger { get; set; }

        public LoggingService()
        {
            Logger = new LoggerConfiguration()
                            .MinimumLevel.Debug()
                            .WriteTo.File("toolkit-diagnostics.log")
                            .CreateLogger();
        }
    }
}
