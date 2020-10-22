using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Security;
using System.Threading.Tasks;
using TanzuForVS.CloudFoundryApiClient;
using TanzuForVS.CloudFoundryApiClient.Models.AppsResponse;
using TanzuForVS.CloudFoundryApiClient.Models.OrgsResponse;
using TanzuForVS.CloudFoundryApiClient.Models.SpacesResponse;
using TanzuForVS.Models;

namespace TanzuForVS.Services.CloudFoundry
{
    public class CloudFoundryService : ICloudFoundryService
    {
        private static ICfApiClient _cfApiClient;
        public string LoginFailureMessage { get; } = "Login failed.";
        public Dictionary<string, CloudFoundryInstance> CloudFoundryInstances { get; private set; }
        public CloudFoundryInstance ActiveCloud { get; set; }

        public CloudFoundryService(IServiceProvider services)
        {
            _cfApiClient = services.GetRequiredService<ICfApiClient>();
            CloudFoundryInstances = new Dictionary<string, CloudFoundryInstance>();
        }

        public void AddCloudFoundryInstance(string name, string apiAddress, string accessToken)
        {
            if (CloudFoundryInstances.ContainsKey(name)) throw new Exception($"The name {name} already exists.");
            CloudFoundryInstances.Add(name, new CloudFoundryInstance(name, apiAddress, accessToken));
        }

        public async Task<ConnectResult> ConnectToCFAsync(string target, string username, SecureString password, string httpProxy, bool skipSsl)
        {
            if (string.IsNullOrEmpty(target)) throw new ArgumentException(nameof(target));
             
            if (string.IsNullOrEmpty(username)) throw new ArgumentException(nameof(username));

            if (password == null) throw new ArgumentNullException(nameof(password));

            try
            {
                string passwordStr = new System.Net.NetworkCredential(string.Empty, password).Password;
                string accessToken = await _cfApiClient.LoginAsync(target, username, passwordStr);

                if (!string.IsNullOrEmpty(accessToken)) return new ConnectResult(true, null, accessToken);

                throw new Exception(LoginFailureMessage);
            }
            catch (Exception e)
            {
                var errorMessages = new List<string>();
                FormatExceptionMessage(e, errorMessages);
                var errorMessage = string.Join(Environment.NewLine, errorMessages.ToArray());
                return new ConnectResult(false, errorMessage, null);
            }
        }

        public async Task<List<CloudFoundryOrganization>> GetOrgsAsync(string target, string accessToken)
        {
            List<Org> orgsResults = await _cfApiClient.ListOrgs(target, accessToken);

            var orgs = new List<CloudFoundryOrganization>();
            if (orgsResults != null)
            {
                orgsResults.ForEach(delegate (Org org) { 
                    orgs.Add(new CloudFoundryOrganization(org.name, org.guid)); 
                });
            }

            return orgs;
        }

        public async Task<List<CloudFoundrySpace>> GetSpacesAsync(string target, string accessToken, string orgId)
        {
            List<Space> spacesResults = await _cfApiClient.ListSpacesWithGuid(target, accessToken, orgId);

            var spaces = new List<CloudFoundrySpace>();
            if (spacesResults != null)
            {
                spacesResults.ForEach(delegate (Space space) { 
                    spaces.Add(new CloudFoundrySpace(space.name, space.guid)); 
                });
            }

            return spaces;
        }

        public async Task<List<CloudFoundryApp>> GetAppsAsync(string target, string accessToken, string spaceId)
        {
            List<App> appResults = await _cfApiClient.ListAppsWithGuid(target, accessToken, spaceId);

            var apps = new List<CloudFoundryApp>();
            if (appResults != null)
            {
                appResults.ForEach(delegate (App app) {
                    apps.Add(new CloudFoundryApp(app.name));
                });
            }

            return apps;
        }

        public static void FormatExceptionMessage(Exception ex, List<string> message)
        {
            var aex = ex as AggregateException;
            if (aex != null)
            {
                foreach (Exception iex in aex.InnerExceptions)
                {
                    FormatExceptionMessage(iex, message);
                }
            }
            else
            {
                message.Add(ex.Message);

                if (ex.InnerException != null)
                {
                    FormatExceptionMessage(ex.InnerException, message);
                }
            }
        }
    }
}
