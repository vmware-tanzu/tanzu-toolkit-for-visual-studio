using Serilog;
using System;
using System.IO;
using System.Threading.Tasks;
using Tanzu.Toolkit.Services.CommandProcess;
using Tanzu.Toolkit.Services.Logging;

namespace Tanzu.Toolkit.Services.DotnetCli
{
    public class DotnetCliService : IDotnetCliService
    {
        private const string _dotnetCliExecutable = "C:\\Program Files\\dotnet\\dotnet.exe";
        private readonly ICommandProcessService _commandProcessService;
        private ILogger _logger;

        public DotnetCliService(ICommandProcessService commandProcessService, ILoggingService loggingService)
        {
            _logger = loggingService.Logger;
            _commandProcessService = commandProcessService;
        }

        /// <summary>
        /// Invokes `dotnet publish` and specifies that debugging symbols should be included.
        /// </summary>
        /// <param name="projectDir"></param>
        /// <param name="targetFrameworkMoniker"></param>
        /// <param name="runtimeIdentifier"></param>
        /// <param name="configuration"></param>
        /// <param name="outputDirName"></param>
        /// 
        /// <returns></returns>
        public async Task<bool> PublishProjectForRemoteDebuggingAsync(string projectDir, string targetFrameworkMoniker, string runtimeIdentifier, string configuration, string outputDirName, Action<string> StdOutCallback = null, Action<string> StdErrCallback = null)
        {
            try
            {
                var expectedOutputDirName = Path.Combine(projectDir, outputDirName);
                if (Directory.Exists(expectedOutputDirName))
                {
                    Directory.Delete(expectedOutputDirName, true); // clean before recreating
                }

                var publishArgs = $"publish -f {targetFrameworkMoniker} -r {runtimeIdentifier} -c {configuration} -o {outputDirName} --self-contained";
                var publishProcess = _commandProcessService.StartProcess(_dotnetCliExecutable, publishArgs, projectDir, stdOutDelegate: StdOutCallback, stdErrDelegate: StdErrCallback);
                await Task.Run(() => publishProcess.WaitForExit());
                return publishProcess.ExitCode == 0;
            }
            catch (Exception ex)
            {
                _logger.Error("Caught exception while trying to invoke dotnet publish: {DotnetPublishException}", ex);
                return false;
            }
        }
    }
}
