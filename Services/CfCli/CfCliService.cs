using Microsoft.Extensions.DependencyInjection;
using System;
using System.IO;
using System.Threading.Tasks;
using Tanzu.Toolkit.VisualStudio.Services.CmdProcess;
using Tanzu.Toolkit.VisualStudio.Services.FileLocator;
using static Tanzu.Toolkit.VisualStudio.Services.OutputHandler.OutputHandler;

namespace Tanzu.Toolkit.VisualStudio.Services.CfCli
{
    public class CfCliService : ICfCliService
    {
        private readonly ICmdProcessService _cmdProcessService;
        private readonly IFileLocatorService _fileLocatorService;

        /* CF CLI V6 COMMANDS */
        public static string V6_GetCliVersionCmd = "version";
        public static string V6_GetOAuthTokenCmd = "oauth-token";

        public CfCliService(IServiceProvider services)
        {
            _cmdProcessService = services.GetRequiredService<ICmdProcessService>();
            _fileLocatorService = services.GetRequiredService<IFileLocatorService>();
        }


        public string GetOAuthToken()
        {
            CmdResult result = ExecuteCfCliCommand(V6_GetOAuthTokenCmd);

            if (result.ExitCode != 0) return null;

            return FormatToken(result.StdOut);
        }

        /// <summary>
        /// Initiate a new Cloud Foundry CLI command with the given arguments.
        /// Invoke the command prompt and wait for the process to exit before returning.
        /// Return true if no StdError was captured over the course of the process, false otherwise.
        /// </summary>
        /// <param name="arguments">Parameters to include along with the `cf` command (e.g. "push", "apps")</param>
        /// <param name="workingDir"></param>
        /// <returns></returns>
        public async Task<DetailedResult> InvokeCfCliAsync(string arguments, StdOutDelegate stdOutHandler, string workingDir = null)
        {
            string pathToCfExe = _fileLocatorService.FullPathToCfExe;
            if (string.IsNullOrEmpty(pathToCfExe)) return new DetailedResult(false, $"Unable to locate cf.exe.");

            string commandStr = '"' + pathToCfExe + '"' + ' ' + arguments;
            bool commandSucceededWithoutError = await _cmdProcessService.InvokeWindowlessCommandAsync(commandStr, workingDir, stdOutHandler);

            if (commandSucceededWithoutError) return new DetailedResult(succeeded: true);

            string reason = $"Unable to execute `cf {arguments}`.";
            return new DetailedResult(false, reason);
        }

        /// <summary>
        /// Invoke the CF CLI command prompt process and return StdOut result string immediately; do not wait for process to exit.
        /// </summary>
        /// <param name="arguments">Parameters to include along with the `cf` command (e.g. "push", "apps")</param>
        /// <param name="workingDir"></param>
        public CmdResult ExecuteCfCliCommand(string arguments, string workingDir = null)
        {
            string pathToCfExe = _fileLocatorService.FullPathToCfExe;
            if (string.IsNullOrEmpty(pathToCfExe)) throw new FileNotFoundException("Unable to locate cf.exe.");

            string commandStr = '"' + pathToCfExe + '"' + ' ' + arguments;
            return _cmdProcessService.ExecuteWindowlessCommand(commandStr, workingDir);
        }

        private string FormatToken(string tokenStr)
        {
            tokenStr = tokenStr.Replace("\n","");
            if (tokenStr.StartsWith("bearer ")) tokenStr = tokenStr.Remove(0, 7);
            
            return tokenStr;
        }
    }
}
