using Serilog;

namespace Tanzu.Toolkit.Services.Logging
{
    public interface ILoggingService
    {
        ILogger Logger { get; }
    }
}