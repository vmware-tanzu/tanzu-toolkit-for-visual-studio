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
        private const string _defaultCfAppDir = "app";

        public VsdbgInstaller(ICfCliService cfClient)
        {
            _cfClient = cfClient;
        }

        public string VsdbgDirName => "vsdbg";

        public async Task<DetailedResult> InstallVsdbgForCFAppAsync(CloudFoundryApp app)
        {
            string scriptExt;
            string startCmd;
            const string vsVersion = "latest";

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
            const string linuxRuntimeId = "linux-x64";
            const string windowsRuntimeId = "win7-x64";
            if (stack != null && stack.Contains("windows"))
            {
                scriptExt = "ps1";
                startCmd = $"powershell -File {_vsdbgInstallScriptName}.{scriptExt} -Version {vsVersion} -RuntimeID {windowsRuntimeId} -InstallPath .\\{VsdbgDirName}";
            }
            else if (stack != null && stack.Contains("linux"))
            {
                scriptExt = "sh";
                var permissionGrant = $"chmod +x {_vsdbgInstallScriptName}.{scriptExt}";
                startCmd = $"{permissionGrant} && ./{_vsdbgInstallScriptName}.{scriptExt} -v {vsVersion} -r {linuxRuntimeId} -l ./{VsdbgDirName}";
            }
            else
            {
                throw new Exception($"Unexpected stack: '{stack}'");
            }
        }
    }
}
