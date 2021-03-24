using Serilog;

namespace Tanzu.Toolkit.VisualStudio.Services.Logging
{
    public interface ILoggingService
    {
        ILogger Logger { get; }
    }
}