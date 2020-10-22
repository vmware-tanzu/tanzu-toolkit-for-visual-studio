using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using TanzuForVS.CloudFoundryApiClient.Models;
using TanzuForVS.CloudFoundryApiClient.Models.AppsResponse;
using TanzuForVS.CloudFoundryApiClient.Models.BasicInfoResponse;
using TanzuForVS.CloudFoundryApiClient.Models.OrgsResponse;
using TanzuForVS.CloudFoundryApiClient.Models.SpacesResponse;

namespace TanzuForVS.CloudFoundryApiClient
{
    public class CfApiClient : ICfApiClient
    {
        public string AccessToken { get; private set; }

        internal static readonly string listOrgsPath = "/v3/organizations";
        internal static readonly string listSpacesPath = "/v3/spaces";
        internal static readonly string listAppsPath = "/v3/apps";

        public static readonly string defaultAuthClientId = "cf";
        public static readonly string defaultAuthClientSecret = "";
        public static readonly string AuthServerLookupFailureMessage = "Unable to locate authentication server";
        public static readonly string InvalidTargetUriMessage = "Invalid target URI";

        private static IUaaClient _uaaClient;
        private static HttpClient _httpClient;

        public CfApiClient(IUaaClient uaaClient, HttpClient httpClient)
        {
            _uaaClient = uaaClient;
            _httpClient = httpClient;
            AccessToken = null;
        }

        /// <summary>
        /// LoginAsync contacts the auth server for the specified cfTarget
        /// </summary>
        /// <param name="cfTarget"></param>
        /// <param name="cfUsername"></param>
        /// <param name="cfPassword"></param>
        /// <returns>
        /// Access Token from the auth server as a string, 
        /// or null if auth request responded with a status code other than 200
        /// </returns>
        public async Task<string> LoginAsync(string cfTarget, string cfUsername, string cfPassword)
        {
            validateUriStringOrThrow(cfTarget, InvalidTargetUriMessage);
            var authServerUri = await GetAuthServerUriFromCfTarget(cfTarget);

            var result = await _uaaClient.RequestAccessTokenAsync(authServerUri,
                                                                  defaultAuthClientId,
                                                                  defaultAuthClientSecret,
                                                                  cfUsername,
                                                                  cfPassword);

            if (result == HttpStatusCode.OK)
            {
                AccessToken = _uaaClient.Token.access_token;
                return AccessToken;
            }
            else
            {
                return null;
            }
        }

        private async Task<Uri> GetAuthServerUriFromCfTarget(string cfTargetString)
        {
            try
            {
                Uri authServerUri = null;

                var uri = new UriBuilder(cfTargetString)
                {
                    Path = "/",
                    Port = -1
                };

                var request = new HttpRequestMessage(HttpMethod.Get, uri.ToString());

                var response = await _httpClient.SendAsync(request);

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var basicInfo = JsonConvert.DeserializeObject<BasicInfoResponse>(content);
                    authServerUri = new Uri(basicInfo.links.login.href);
                }

                return authServerUri;
            }
            catch (Exception e)
            {
                e.Data.Add("MessageToDisplay", AuthServerLookupFailureMessage);
                throw;
            }
        }

        private Uri validateUriStringOrThrow(string uriString, string errorMessage)
        {
            Uri uriResult;
            Uri.TryCreate(uriString, UriKind.Absolute, out uriResult);

            if (uriResult == null) throw new Exception(errorMessage);

            return uriResult;
        }

        public async Task<List<Org>> ListOrgs(string cfTarget, string accessToken)
        {
            try
            {
                // trust any certificate
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                ServicePointManager.ServerCertificateValidationCallback +=
                    (sender, cert, chain, sslPolicyErrors) => { return true; };

                var uri = new UriBuilder(cfTarget);
                uri.Path = listOrgsPath;

                var request = new HttpRequestMessage(HttpMethod.Get, uri.ToString());
                request.Headers.Add("Authorization", "Bearer " + accessToken);

                var response = await _httpClient.SendAsync(request);

                if (!response.IsSuccessStatusCode) throw new Exception($"Response from `{listOrgsPath}` was {response.StatusCode}");

                string resultContent = await response.Content.ReadAsStringAsync();
                var orgsResponse = JsonConvert.DeserializeObject<OrgsResponse>(resultContent);

                List<Org> visibleOrgs = orgsResponse.Orgs.ToList();

                if (orgsResponse.pagination.next != null)
                {
                    visibleOrgs = await GetRemainingOrgsPages(orgsResponse.pagination.next, accessToken, visibleOrgs);
                }

                return visibleOrgs;
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine(e);
                return null;
            }

        }

        public async Task<List<Space>> ListSpaces(string cfTarget, string accessToken)
        {
            try
            {
                // trust any certificate
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                ServicePointManager.ServerCertificateValidationCallback +=
                    (sender, cert, chain, sslPolicyErrors) => { return true; };

                var uri = new UriBuilder(cfTarget);
                uri.Path = listSpacesPath;

                var request = new HttpRequestMessage(HttpMethod.Get, uri.ToString());
                request.Headers.Add("Authorization", "Bearer " + accessToken);

                var response = await _httpClient.SendAsync(request);

                if (!response.IsSuccessStatusCode) throw new Exception($"Response from `{listSpacesPath}` was {response.StatusCode}");

                string resultContent = await response.Content.ReadAsStringAsync();
                var spacesResponse = JsonConvert.DeserializeObject<SpacesResponse>(resultContent);

                List<Space> visibleSpaces = spacesResponse.Spaces.ToList();

                if (spacesResponse.pagination.next != null)
                {
                    visibleSpaces = await GetRemainingSpacesPages(spacesResponse.pagination.next, accessToken, visibleSpaces);
                }

                return visibleSpaces;
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine(e);
                return null;
            }

        }

        private async Task<List<Org>> GetRemainingOrgsPages(Href nextPageHref, string accessToken, List<Org> resourcesList)
        {
            if (nextPageHref == null) return resourcesList;

            var request = new HttpRequestMessage(HttpMethod.Get, nextPageHref.href);
            request.Headers.Add("Authorization", "Bearer " + accessToken);

            var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode) throw new Exception($"Response from `{listOrgsPath}` was {response.StatusCode}");

            string resultContent = await response.Content.ReadAsStringAsync();
            var orgsResponse = JsonConvert.DeserializeObject<OrgsResponse>(resultContent);

            resourcesList.AddRange(orgsResponse.Orgs.ToList());

            return await GetRemainingOrgsPages(orgsResponse.pagination.next, accessToken, resourcesList);
        }

        private async Task<List<Space>> GetRemainingSpacesPages(Href nextPageHref, string accessToken, List<Space> resourcesList)
        {
            if (nextPageHref == null) return resourcesList;

            var request = new HttpRequestMessage(HttpMethod.Get, nextPageHref.href);
            request.Headers.Add("Authorization", "Bearer " + accessToken);

            var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode) throw new Exception($"Response from `{listSpacesPath}` was {response.StatusCode}");

            string resultContent = await response.Content.ReadAsStringAsync();
            var spacesResponse = JsonConvert.DeserializeObject<SpacesResponse>(resultContent);

            resourcesList.AddRange(spacesResponse.Spaces.ToList());

            return await GetRemainingSpacesPages(spacesResponse.pagination.next, accessToken, resourcesList);
        }

        public async Task<List<Space>> ListSpacesWithGuid(string cfTarget, string accessToken, string orgGuid)
        {
            try
            {
                // trust any certificate
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                ServicePointManager.ServerCertificateValidationCallback +=
                    (sender, cert, chain, sslPolicyErrors) => { return true; };

                var uri = new UriBuilder(cfTarget)
                {
                    Path = listSpacesPath,
                    Query = $"organization_guids={orgGuid}"
                };

                var request = new HttpRequestMessage(HttpMethod.Get, uri.ToString());
                request.Headers.Add("Authorization", "Bearer " + accessToken);

                var response = await _httpClient.SendAsync(request);

                if (!response.IsSuccessStatusCode)
                {
                    throw new Exception($"Response from `{listSpacesPath}?organization_guids={orgGuid}` was {response.StatusCode}");
                }

                string resultContent = await response.Content.ReadAsStringAsync();
                var spacesResponse = JsonConvert.DeserializeObject<SpacesResponse>(resultContent);

                List<Space> visibleSpaces = spacesResponse.Spaces.ToList();

                if (spacesResponse.pagination.next != null)
                {
                    visibleSpaces = await GetRemainingSpacesPages(spacesResponse.pagination.next, accessToken, visibleSpaces);
                }

                return visibleSpaces;
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine(e);
                return null;
            }

        }

        public async Task<List<App>> ListAppsWithGuid(string cfTarget, string accessToken, string spaceGuid)
        {
            try
            {
                // trust any certificate
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                ServicePointManager.ServerCertificateValidationCallback +=
                    (sender, cert, chain, sslPolicyErrors) => { return true; };

                var uri = new UriBuilder(cfTarget)
                {
                    Path = listAppsPath,
                    Query = $"space_guids={spaceGuid}"
                };

                var request = new HttpRequestMessage(HttpMethod.Get, uri.ToString());
                request.Headers.Add("Authorization", "Bearer " + accessToken);

                var response = await _httpClient.SendAsync(request);

                if (!response.IsSuccessStatusCode)
                {
                    throw new Exception($"Response from `{listAppsPath}?space_guids={spaceGuid}` was {response.StatusCode}");
                }

                string resultContent = await response.Content.ReadAsStringAsync();
                var appsResponse = JsonConvert.DeserializeObject<AppsResponse>(resultContent);

                List<App> visibleApps = appsResponse.Apps.ToList();

                if (appsResponse.pagination.next != null)
                {
                    visibleApps = await GetRemainingAppsPages(appsResponse.pagination.next, accessToken, visibleApps);
                }

                return visibleApps;
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine(e);
                return null;
            }

        }

        private async Task<List<App>> GetRemainingAppsPages(Href nextPageHref, string accessToken, List<App> resourcesList)
        {
            if (nextPageHref == null) return resourcesList;

            var request = new HttpRequestMessage(HttpMethod.Get, nextPageHref.href);
            request.Headers.Add("Authorization", "Bearer " + accessToken);

            var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode) throw new Exception($"Response from `{listAppsPath}` was {response.StatusCode}");

            string resultContent = await response.Content.ReadAsStringAsync();
            var appsResponse = JsonConvert.DeserializeObject<AppsResponse>(resultContent);

            resourcesList.AddRange(appsResponse.Apps.ToList());

            return await GetRemainingAppsPages(appsResponse.pagination.next, accessToken, resourcesList);
        }
    }
}
