using Serilog;
using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using Tanzu.Toolkit.Services.CommandProcess;
using Tanzu.Toolkit.Services.File;
using Tanzu.Toolkit.Services.Logging;

namespace Tanzu.Toolkit.Services.DotnetCli
{
    public class DotnetCliService : IDotnetCliService
    {
        private const string _dotnetCliExecutable = "C:\\Program Files\\dotnet\\dotnet.exe";
        private readonly ICommandProcessService _commandProcessService;
        private readonly IFileService _fileService;
        private ILogger _logger;

        public DotnetCliService(ICommandProcessService commandProcessService, ILoggingService loggingService, IFileService fileService)
        {
            _logger = loggingService.Logger;
            _commandProcessService = commandProcessService;
            _fileService = fileService;
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
                // dotnet publish
                var publishDirPath = Path.Combine(projectDir, outputDirName);
                if (Directory.Exists(publishDirPath))
                {
                    Directory.Delete(publishDirPath, true); // clean before recreating
                }
                var publishArgs = $"publish -f {targetFrameworkMoniker} -r {runtimeIdentifier} -c {configuration} -o {outputDirName} --self-contained";
                var publishProcess = _commandProcessService.StartProcess(_dotnetCliExecutable, publishArgs, projectDir, stdOutDelegate: StdOutCallback, stdErrDelegate: StdErrCallback);
                await Task.Run(() => publishProcess.WaitForExit());

                // get vsdbg installer
                var vsdbgDownloadUrl = "https://aka.ms/getvsdbgps1";
                const string installerName = "GetVsDbg.ps1";
                var installScriptPath = Path.Combine(projectDir, outputDirName, installerName);
                using (var client = new WebClient())
                {
                    await client.DownloadFileTaskAsync(vsdbgDownloadUrl, installScriptPath);
                }

                // install vsdbg into publish dir
                var vsdbgInstallationDirName = "vsdbg";
                var vsdbgVersion = "latest";
                var installerArgs = $"-File \"{installerName}\" -Version {vsdbgVersion} -InstallPath {vsdbgInstallationDirName}/";
                var installationProcess = _commandProcessService.StartProcess("powershell.exe", installerArgs, publishDirPath, stdOutDelegate: StdOutCallback, stdErrDelegate: StdErrCallback);
                await Task.Run(() => installationProcess.WaitForExit());

                _fileService.DeleteFile(installScriptPath);

                return publishProcess.ExitCode == 0 && installationProcess.ExitCode == 0;
            }
            catch (Exception ex)
            {
                _logger.Error("Caught exception while trying to invoke dotnet publish: {DotnetPublishException}", ex);
                return false;
            }
        }
    }
}
