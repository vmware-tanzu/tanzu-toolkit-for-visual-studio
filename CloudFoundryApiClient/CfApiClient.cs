using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Tanzu.Toolkit.CloudFoundryApiClient.Models;
using Tanzu.Toolkit.CloudFoundryApiClient.Models.AppsResponse;
using Tanzu.Toolkit.CloudFoundryApiClient.Models.BasicInfoResponse;
using Tanzu.Toolkit.CloudFoundryApiClient.Models.OrgsResponse;
using Tanzu.Toolkit.CloudFoundryApiClient.Models.SpacesResponse;

namespace Tanzu.Toolkit.CloudFoundryApiClient
{
    public class CfApiClient : ICfApiClient
    {
        public string AccessToken { get; private set; }

        internal const string listOrgsPath = "/v3/organizations";
        internal const string listSpacesPath = "/v3/spaces";
        internal const string listAppsPath = "/v3/apps";
        internal const string deleteAppsPath = "/v3/apps";

        public const string defaultAuthClientId = "cf";
        public const string defaultAuthClientSecret = "";
        public const string AuthServerLookupFailureMessage = "Unable to locate authentication server";
        public const string InvalidTargetUriMessage = "Invalid target URI";

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

        /// <summary>
        /// Recursively requests all pages of results for orgs on the Cloud Foundry instance as specified by <paramref name="cfApiAddress"/>.
        /// <para>
        /// Exceptions:
        /// <para>
        /// Throws any exceptions encountered while creating/issuing request or deserializing response.
        /// </para>
        /// </para>
        /// </summary>
        /// <param name="cfApiAddress"></param>
        /// <param name="accessToken"></param>
        /// <returns>List of <see cref="Org"/>s.</returns>
        public async Task<List<Org>> ListOrgs(string cfApiAddress, string accessToken)
        {
            // trust any certificate
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            ServicePointManager.ServerCertificateValidationCallback +=
                (sender, cert, chain, sslPolicyErrors) => { return true; };

            var uri = new UriBuilder(cfApiAddress)
            {
                Path = listOrgsPath,
            };

            Href firstPageHref = new Href() { href = uri.ToString() };

            List<Org> visibleOrgs = await GetRemainingPagesForType(firstPageHref, accessToken, new List<Org>());

            return visibleOrgs;
        }

        public async Task<List<Space>> ListSpaces(string cfTarget, string accessToken)
        {
            try
            {
                // trust any certificate
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                ServicePointManager.ServerCertificateValidationCallback +=
                    (sender, cert, chain, sslPolicyErrors) => { return true; };

                var uri = new UriBuilder(cfTarget)
                {
                    Path = listSpacesPath
                };

                Href firstPageHref = new Href() { href = uri.ToString() };

                return await GetRemainingPagesForType(firstPageHref, accessToken, new List<Space>());
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine(e);
                return null;
            }
        }

        /// <summary>
        /// Recursively requests all pages of results for spaces under the org specified by <paramref name="orgGuid"/>.
        /// <para>
        /// Exceptions:
        /// <para>
        /// Throws any exceptions encountered while creating/issuing request or deserializing response.
        /// </para>
        /// </para>
        /// </summary>
        /// <param name="cfApiAddress"></param>
        /// <param name="accessToken"></param>
        /// <param name="orgGuid"></param>
        /// <returns>List of <see cref="Space"/>s.</returns>
        public async Task<List<Space>> ListSpacesForOrg(string cfApiAddress, string accessToken, string orgGuid)
        {
            // trust any certificate
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            ServicePointManager.ServerCertificateValidationCallback +=
                (sender, cert, chain, sslPolicyErrors) => { return true; };

            var uri = new UriBuilder(cfApiAddress)
            {
                Path = listSpacesPath,
                Query = $"organization_guids={orgGuid}"
            };

            Href firstPageHref = new Href() { href = uri.ToString() };

            return await GetRemainingPagesForType(firstPageHref, accessToken, new List<Space>());
        }

        /// <summary>
        /// Recursively requests all pages of results for apps under the space specified by <paramref name="spaceGuid"/>.
        /// <para>
        /// Exceptions:
        /// <para>
        /// Throws any exceptions encountered while creating/issuing request or deserializing response.
        /// </para>
        /// </para>
        /// </summary>
        /// <param name="cfTarget"></param>
        /// <param name="accessToken"></param>
        /// <param name="spaceGuid"></param>
        /// <returns>List of <see cref="App"/>s.</returns>
        public async Task<List<App>> ListAppsForSpace(string cfTarget, string accessToken, string spaceGuid)
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

            Href firstPageHref = new Href() { href = uri.ToString() };

            return await GetRemainingPagesForType(firstPageHref, accessToken, new List<App>());
        }

        /// <summary>
        /// Issues a request to <paramref name="cfApiAddress"/> to stop the app specified by <paramref name="appGuid"/>.
        /// <para>
        /// Exceptions:
        /// <para>
        /// Throws any exceptions encountered while creating/issuing request or deserializing response.
        /// </para>
        /// </para>
        /// </summary>
        /// <param name="cfApiAddress"></param>
        /// <param name="accessToken"></param>
        /// <param name="appGuid"></param>
        /// <returns>
        /// True if response status code indicates success and response contains an app state of "STOPPED".
        /// <para>False otherwise.</para>
        /// </returns>
        public async Task<bool> StopAppWithGuid(string cfApiAddress, string accessToken, string appGuid)
        {
            // trust any certificate
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            ServicePointManager.ServerCertificateValidationCallback +=
                (sender, cert, chain, sslPolicyErrors) => { return true; };

            var stopAppPath = listAppsPath + $"/{appGuid}/actions/stop";

            var uri = new UriBuilder(cfApiAddress)
            {
                Path = stopAppPath
            };

            var request = new HttpRequestMessage(HttpMethod.Post, uri.ToString());
            request.Headers.Add("Authorization", "Bearer " + accessToken);

            var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode) throw new Exception($"Response from POST `{stopAppPath}` was {response.StatusCode}");

            string resultContent = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<App>(resultContent);

            if (result.state == "STOPPED") return true;
            return false;
        }

        /// <summary>
        /// Issues a request to <paramref name="cfApiAddress"/> to start the app specified by <paramref name="appGuid"/>.
        /// <para>
        /// Exceptions:
        /// <para>
        /// Throws any exceptions encountered while creating/issuing request or deserializing response.
        /// </para>
        /// </para>
        /// </summary>
        /// <param name="cfApiAddress"></param>
        /// <param name="accessToken"></param>
        /// <param name="appGuid"></param>
        /// <returns>
        /// True if response status code indicates success and response contains an app state of "STARTED".
        /// <para>False otherwise.</para>
        /// </returns>
        public async Task<bool> StartAppWithGuid(string cfApiAddress, string accessToken, string appGuid)
        {
            // trust any certificate
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            ServicePointManager.ServerCertificateValidationCallback +=
                (sender, cert, chain, sslPolicyErrors) => { return true; };

            var startAppPath = listAppsPath + $"/{appGuid}/actions/start";

            var uri = new UriBuilder(cfApiAddress)
            {
                Path = startAppPath
            };

            var request = new HttpRequestMessage(HttpMethod.Post, uri.ToString());
            request.Headers.Add("Authorization", "Bearer " + accessToken);

            var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode) throw new Exception($"Response from POST `{startAppPath}` was {response.StatusCode}");

            string resultContent = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<App>(resultContent);

            if (result.state == "STARTED") return true;
            return false;
        }

        public async Task<bool> DeleteAppWithGuid(string cfTarget, string accessToken, string appGuid)
        {
            try
            {
                // trust any certificate
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                ServicePointManager.ServerCertificateValidationCallback +=
                    (sender, cert, chain, sslPolicyErrors) => { return true; };

                var deleteAppPath = deleteAppsPath + $"/{appGuid}";

                var uri = new UriBuilder(cfTarget)
                {
                    Path = deleteAppPath
                };

                var request = new HttpRequestMessage(HttpMethod.Delete, uri.ToString());
                request.Headers.Add("Authorization", "Bearer " + accessToken);

                var response = await _httpClient.SendAsync(request);
                if (response.StatusCode != HttpStatusCode.Accepted) throw new Exception($"Response from DELETE `{deleteAppPath}` was {response.StatusCode}");

                if (response.StatusCode == HttpStatusCode.Accepted) return true;
                return false;
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine(e);
                return false;
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

        private async Task<List<ResourceType>> GetRemainingPagesForType<ResourceType>(Href pageAddress, string accessToken, List<ResourceType> resultsSoFar)
        {
            if (pageAddress == null) return resultsSoFar;

            var request = new HttpRequestMessage(HttpMethod.Get, pageAddress.href);
            request.Headers.Add("Authorization", "Bearer " + accessToken);

            var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode) throw new Exception($"Response from GET `{pageAddress}` was {response.StatusCode}");

            string resultContent = await response.Content.ReadAsStringAsync();

            Href nextPageHref;

            if (typeof(ResourceType) == typeof(Org))
            {
                var results = JsonConvert.DeserializeObject<OrgsResponse>(resultContent);
                resultsSoFar.AddRange((IEnumerable<ResourceType>)results.Orgs.ToList());

                nextPageHref = results.Pagination.next;
            }
            else if (typeof(ResourceType) == typeof(Space))
            {
                var results = JsonConvert.DeserializeObject<SpacesResponse>(resultContent);
                resultsSoFar.AddRange((IEnumerable<ResourceType>)results.Spaces.ToList());

                nextPageHref = results.Pagination.next;
            }
            else if (typeof(ResourceType) == typeof(App))
            {
                var results = JsonConvert.DeserializeObject<AppsResponse>(resultContent);
                resultsSoFar.AddRange((IEnumerable<ResourceType>)results.Apps.ToList());

                nextPageHref = results.Pagination.next;
            }
            else
            {
                throw new Exception($"ResourceType unknown: {typeof(ResourceType).Name}");
            }

            return await GetRemainingPagesForType(nextPageHref, accessToken, resultsSoFar);
        }

    }
}
