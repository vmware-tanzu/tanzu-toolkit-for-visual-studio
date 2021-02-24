using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Tanzu.Toolkit.VisualStudio.Services.CfCli.Models.Orgs;
using Tanzu.Toolkit.VisualStudio.Services.CfCli.Models.Spaces;
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

        /* CF CLI V6 CONSTANTS */
        public static string V6_GetCliVersionCmd = "version";
        public static string V6_GetOAuthTokenCmd = "oauth-token";
        public static string V6_TargetApiCmd = "api";
        public static string V6_AuthenticateCmd = "auth";
        public static string V6_TargetOrgCmd = "target -o";
        public static string V6_GetOrgsCmd = "orgs";
        public static string V6_GetSpacesCmd = "spaces";
        internal static string V6_GetOrgsRequestPath = "GET /v2/organizations";
        internal static string V6_GetSpacesRequestPath = "GET /v2/spaces";


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

        public DetailedResult TargetOrg(string orgName)
        {
            string args = $"{V6_TargetOrgCmd} {orgName}";
            return ExecuteCfCliCommand(args);
        }

        public async Task<List<Org>> GetOrgsAsync()
        {
            try
            {
                string args = $"{V6_GetOrgsCmd} -v"; // -v prints api request details to stdout
                DetailedResult result = await InvokeCfCliAsync(args);

                if (!result.Succeeded || result.CmdDetails.ExitCode != 0) return new List<Org>();

                /* break early & skip json parsing if output contains 'No orgs found' */
                string content = result.CmdDetails.StdOut;
                string contentEnding = content.Substring(content.Length - 20);
                if (contentEnding.Contains("No orgs found")) return new List<Org>();

                var orgResponsePages = GetJsonResponsePages<OrgsApiV2ResponsePage>(result.CmdDetails.StdOut, V6_GetOrgsRequestPath);

                var orgsList = new List<Org>();
                foreach (OrgsApiV2ResponsePage responsePage in orgResponsePages)
                {
                    foreach (Org org in responsePage.resources)
                    {
                        orgsList.Add(org);
                    }
                }

                return orgsList;
            }
            catch
            {
                return new List<Org>();
            }
        }

        public async Task<List<Space>> GetSpacesAsync()
        {
            try
            {
                string args = $"{V6_GetSpacesCmd} -v"; // -v prints api request details to stdout
                DetailedResult result = await InvokeCfCliAsync(args);

                if (!result.Succeeded || result.CmdDetails.ExitCode != 0) return new List<Space>();

                /* break early & skip json parsing if output contains 'No spaces found' */
                string content = result.CmdDetails.StdOut;
                string contentEnding = content.Substring(content.Length - 20);
                if (contentEnding.Contains("No spaces found")) return new List<Space>();

                var spaceResponsePages = GetJsonResponsePages<SpacesApiV2ResponsePage>(content, V6_GetSpacesRequestPath);

                var spacesList = new List<Space>();
                foreach (SpacesApiV2ResponsePage responsePage in spaceResponsePages)
                {
                    foreach (Space space in responsePage.resources)
                    {
                        spacesList.Add(space);
                    }
                }

                return spacesList;
            }
            catch
            {
                return new List<Space>();
            }
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

        internal List<ResponseType> GetJsonResponsePages<ResponseType>(string content, string requestFilter)
        {
            try
            {
                var jsonResponses = new List<ResponseType>();

                /* separate content into individual request/response pairs */
                string[] requestResponsePairs = Regex.Split(content, pattern: "(?=REQUEST)");

                /* filter out extraneous requests */
                List<string> relevantRequests = requestResponsePairs
                    .Where(s => s.Contains(requestFilter))
                    .ToList();

                foreach (string requestResponsePair in relevantRequests)
                {
                    if (!string.IsNullOrWhiteSpace(requestResponsePair))
                    {
                        /* separate request from response */
                        string[] reqRes = Regex.Split(requestResponsePair, pattern: "(?=RESPONSE)");

                        string responseContent = reqRes[1];

                        /* look for any part of the response content that resembles json */
                        if (responseContent.Contains("{") && responseContent.Contains("}"))
                        {
                            int substringStart = responseContent.IndexOf('{');
                            int substringEnd = responseContent.LastIndexOf('}');
                            int substringLength = substringEnd - substringStart + 1;

                            string speculativeJsonStr = responseContent.Substring(
                                substringStart,
                                substringLength);

                            /* try converting json-like string to `ResponseType` instance */
                            ResponseType responsePage = JsonConvert.DeserializeObject<ResponseType>(speculativeJsonStr);

                            jsonResponses.Add(responsePage);
                        }
                    }
                }

                return jsonResponses;
            }
            catch
            {
                return new List<ResponseType>();
            }
        }
    }
}
