using Microsoft.Extensions.DependencyInjection;
using System;
using System.IO;
using System.Security;
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

        internal readonly string cfExePathErrorMsg = $"Unable to locate cf.exe.";

        /* CF CLI V6 COMMANDS */
        public static string V6_GetCliVersionCmd = "version";
        public static string V6_GetOAuthTokenCmd = "oauth-token";
        public static string V6_TargetApiCmd = "api";
        public static string V6_AuthenticateCmd = "auth";

        public CfCliService(IServiceProvider services)
        {
            _cmdProcessService = services.GetRequiredService<ICmdProcessService>();
            _fileLocatorService = services.GetRequiredService<IFileLocatorService>();
        }


        public string GetOAuthToken()
        {
            DetailedResult result = ExecuteCfCliCommand(V6_GetOAuthTokenCmd);

            if (result.CmdDetails.ExitCode != 0) return null;

            return FormatToken(result.CmdDetails.StdOut);
        }

        public DetailedResult TargetApi(string apiAddress, bool skipSsl)
        {
            string args = $"{V6_TargetApiCmd} {apiAddress}{(skipSsl ? " --skip-ssl-validation" : string.Empty)}";
            return ExecuteCfCliCommand(args);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0059:Unnecessary assignment of a value", Justification = "Null assigment is meant to clear plain text password from memory")]
        public async Task<DetailedResult> AuthenticateAsync(string username, SecureString password)
        {
            string passwordStr = new System.Net.NetworkCredential(string.Empty, password).Password;

            string args = $"{V6_AuthenticateCmd} {username} {passwordStr}";
            DetailedResult result = await InvokeCfCliAsync(args);

            /* Erase pw from memory */
            passwordStr = null;
            password.Clear();
            password.Dispose();

            return result;
        }

        /// <summary>
        /// Initiate a new Cloud Foundry CLI command with the given arguments.
        /// Invoke the command prompt and wait for the process to exit before returning.
        /// Return true if no StdError was captured over the course of the process, false otherwise.
        /// </summary>
        /// <param name="arguments">Parameters to include along with the `cf` command (e.g. "push", "apps")</param>
        /// <param name="workingDir"></param>
        /// <returns></returns>
        public async Task<DetailedResult> InvokeCfCliAsync(string arguments, StdOutDelegate stdOutCallback = null, StdErrDelegate stdErrCallback = null, string workingDir = null)
        {
            string pathToCfExe = _fileLocatorService.FullPathToCfExe;
            if (string.IsNullOrEmpty(pathToCfExe)) return new DetailedResult(false, $"Unable to locate cf.exe.");

            string commandStr = '"' + pathToCfExe + '"' + ' ' + arguments;
            CmdResult result = await _cmdProcessService.InvokeWindowlessCommandAsync(commandStr, workingDir, stdOutCallback, stdErrCallback);

            if (result.ExitCode == 0) return new DetailedResult(succeeded: true, cmdDetails: result);

            string reason = $"Unable to execute `cf {arguments}`.";
            return new DetailedResult(false, reason, cmdDetails: result);
        }

        /// <summary>
        /// Invoke the CF CLI command prompt process and return StdOut result string immediately; do not wait for process to exit.
        /// </summary>
        /// <param name="arguments">Parameters to include along with the `cf` command (e.g. "push", "apps")</param>
        /// <param name="workingDir"></param>
        public DetailedResult ExecuteCfCliCommand(string arguments, string workingDir = null)
        {
            string pathToCfExe = _fileLocatorService.FullPathToCfExe;
            if (string.IsNullOrEmpty(pathToCfExe))
            {
                return new DetailedResult(false, cfExePathErrorMsg);
            }

            string commandStr = '"' + pathToCfExe + '"' + ' ' + arguments;
            CmdResult result = _cmdProcessService.ExecuteWindowlessCommand(commandStr, workingDir);

            if (result.ExitCode == 0) return new DetailedResult(succeeded: true, cmdDetails: result);

            string reason = $"Unable to execute `cf {arguments}`.";
            return new DetailedResult(false, reason, cmdDetails: result);
        }

        private string FormatToken(string tokenStr)
        {
            tokenStr = tokenStr.Replace("\n", "");
            if (tokenStr.StartsWith("bearer ")) tokenStr = tokenStr.Remove(0, 7);

            return tokenStr;
        }
    }
}
