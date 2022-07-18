using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Tanzu.Toolkit.Models;
using Tanzu.Toolkit.Services;
using Tanzu.Toolkit.Services.CfCli;
using Tanzu.Toolkit.Services.DebugAgentProvider;

[assembly: InternalsVisibleTo("Tanzu.Toolkit.VisualStudioExtension.Tests")]

namespace Tanzu.Toolkit.VisualStudio.Services
{
    public class VsdbgInstaller : IDebugAgentProvider
    {
        internal const string _vsdbgInstallScriptName = "GetVsDbg";
        private ICfCliService _cfClient;
        private string _defaultCfAppDir = "app";

        public VsdbgInstaller(ICfCliService cfClient)
        {
            _cfClient = cfClient;
        }

        public async Task<DetailedResult> InstallVsdbgForCFAppAsync(CloudFoundryApp app, string vsVersion)
        {
            string scriptExt;
            string startCmd;

            if (string.IsNullOrWhiteSpace(vsVersion))
            {
                vsVersion = "latest";
            }

            var stack = app.Stack;
            try
            {
                SetArgsBasedOnStack(stack, vsVersion, out scriptExt, out startCmd);
            }
            catch (Exception ex)
            {
                return new DetailedResult
                {
                    Succeeded = false,
                    Explanation = ex.Message,
                };
            }

            var sshCmd = $"cd {_defaultCfAppDir} && curl -L https://aka.ms/getvsdbg{scriptExt} -o {_vsdbgInstallScriptName}.{scriptExt} && {startCmd}";

            return await _cfClient.ExecuteSshCommand(app.AppName, app.ParentSpace.ParentOrg.OrgName, app.ParentSpace.SpaceName, sshCmd);
        }

        private void SetArgsBasedOnStack(string stack, string vsVersion, out string scriptExt, out string startCmd)
        {
            var linuxRuntimeId = "linux-x64";
            var windowsRuntimeId = "win-x64";
            if (stack != null && stack.Contains("windows"))
            {
                scriptExt = "ps1";
                startCmd = $"powershell -File {_vsdbgInstallScriptName}.ps1 -Version {vsVersion} -RuntimeID {windowsRuntimeId}";
            }
            else if (stack != null && stack.Contains("linux"))
            {
                scriptExt = "sh";
                startCmd = $"./{_vsdbgInstallScriptName}.sh -v {vsVersion} -r {linuxRuntimeId}";
            }
            else
            {
                throw new Exception($"Unexpected stack: '{stack}'");
            }
        }
    }
}
