﻿using Serilog;
using Tanzu.Toolkit.Services.File;

namespace Tanzu.Toolkit.Services.Logging
{
    public class LoggingService : ILoggingService
    {
        public ILogger Logger { get; }

        public LoggingService(IFileService fileService)
        {
            Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.File(
                    path: fileService.PathToLogsFile,
                    shared: true, // allow multiple processes to share same log file
                    fileSizeLimitBytes: 32768, // 32 KiB
                    rollOnFileSizeLimit: true,
                    retainedFileCountLimit: 8)
                .CreateLogger();

            Logger.Information("Logging Service Initialized");
        }
    }
}
