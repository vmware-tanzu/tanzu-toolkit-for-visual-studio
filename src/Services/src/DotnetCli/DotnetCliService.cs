using Serilog;
using System;
using System.Threading.Tasks;
using Tanzu.Toolkit.Services.Logging;

namespace Tanzu.Toolkit.Services.DotnetCli
{
    public class DotnetCliService : IDotnetCliService
    {
        private const string _dotnetCliExecutable = "C:\\Program Files\\dotnet\\dotnet.exe";
        private ILogger _logger;

        public DotnetCliService(ILoggingService loggingService)
        {
            _logger = loggingService.Logger;
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
        public async Task<bool> PublishProjectForRemoteDebuggingAsync(string projectDir, string targetFrameworkMoniker, string runtimeIdentifier, string configuration, string outputDirName)
        {
            try
            {
                using (var publishProcess = new System.Diagnostics.Process())
                {
                    publishProcess.StartInfo.WorkingDirectory = projectDir;
                    publishProcess.StartInfo.FileName = _dotnetCliExecutable;
                    publishProcess.StartInfo.Arguments = $"publish -f {targetFrameworkMoniker} -r {runtimeIdentifier} -c {configuration} -o {outputDirName} --self-contained";
                    publishProcess.StartInfo.UseShellExecute = false;
                    publishProcess.StartInfo.CreateNoWindow = true;
                    publishProcess.Start();
                    await Task.Run(() => publishProcess.WaitForExit());
                }
                return true;
            }
            catch (Exception ex)
            {
                _logger.Error("Caught exception while trying to invoke dotnet publish: {DotnetPublishException}", ex);
                return false;
            }
        }
    }
}
