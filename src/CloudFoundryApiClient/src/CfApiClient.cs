using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Tanzu.Toolkit.CloudFoundryApiClient.Models;
using Tanzu.Toolkit.CloudFoundryApiClient.Models.AppsResponse;
using Tanzu.Toolkit.CloudFoundryApiClient.Models.BasicInfoResponse;
using Tanzu.Toolkit.CloudFoundryApiClient.Models.OrgsResponse;
using Tanzu.Toolkit.CloudFoundryApiClient.Models.SpacesResponse;
using Tanzu.Toolkit.CloudFoundryApiClient.Models.StacksResponse;

namespace Tanzu.Toolkit.CloudFoundryApiClient
{
    public class CfApiClient : ICfApiClient
    {
        public string AccessToken { get; private set; }

        internal const string ListOrgsPath = "/v3/organizations";
        internal const string ListSpacesPath = "/v3/spaces";
        internal const string ListAppsPath = "/v3/apps";
        internal const string ListBuildpacksPath = "/v3/buildpacks";
        internal const string DeleteAppsPath = "/v3/apps";
        internal const string ListStacksPath = "/v3/stacks";
        internal const string LoginInfoPath = "/login"; // the /login endpoint should be identical to the /info endpoint (for CF UAA v 75.10.0)

        internal const string DefaultAuthClientId = "cf";
        internal const string DefaultAuthClientSecret = "";
        internal const string AuthServerLookupFailureMessage = "Unable to locate authentication server";
        internal const string InvalidTargetUriMessage = "Invalid target URI";
        private readonly IUaaClient _uaaClient;
        private readonly HttpClient _httpClient;

        public CfApiClient(IUaaClient uaaClient, HttpClient httpClient)
        {
            _uaaClient = uaaClient;
            _httpClient = httpClient;
            AccessToken = null;
        }

        /// <summary>
        /// LoginAsync contacts the auth server for the specified cfTarget.
        /// </summary>
        /// <param name="cfTarget"></param>
        /// <param name="cfUsername"></param>
        /// <param name="cfPassword"></param>
        /// <returns>
        /// Access Token from the auth server as a string,
        /// or null if auth request responded with a status code other than 200.
        /// </returns>
        public async Task<string> LoginAsync(string cfTarget, string cfUsername, string cfPassword)
        {
            ValidateUriStringOrThrow(cfTarget, InvalidTargetUriMessage);
            var authServerUri = await GetAuthServerUriFromCfTarget(cfTarget);

            var result = await _uaaClient.RequestAccessTokenAsync(authServerUri,
                                                                  DefaultAuthClientId,
                                                                  DefaultAuthClientSecret,
                                                                  cfUsername,
                                                                  cfPassword);

            if (result == HttpStatusCode.OK)
            {
                AccessToken = _uaaClient.Token.Access_token;
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
                Path = ListOrgsPath,
            };

            HypertextReference firstPageHref = new HypertextReference() { Href = uri.ToString() };

            List<Org> visibleOrgs = await GetRemainingPagesForType(firstPageHref, accessToken, new List<Org>());

            return visibleOrgs;
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
                Path = ListSpacesPath,
                Query = $"organization_guids={orgGuid}",
            };

            HypertextReference firstPageHref = new HypertextReference() { Href = uri.ToString() };

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
                Path = ListAppsPath,
                Query = $"space_guids={spaceGuid}",
            };

            HypertextReference firstPageHref = new HypertextReference() { Href = uri.ToString() };

            return await GetRemainingPagesForType(firstPageHref, accessToken, new List<App>());
        }

        public async Task<List<Buildpack>> ListBuildpacks(string cfApiAddress, string accessToken)
        {
            // trust any certificate
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            ServicePointManager.ServerCertificateValidationCallback +=
                (sender, cert, chain, sslPolicyErrors) => { return true; };

            var uri = new UriBuilder(cfApiAddress)
            {
                Path = ListBuildpacksPath,
            };

            HypertextReference firstPageHref = new HypertextReference() { Href = uri.ToString() };

            List<Buildpack> visibleBuildpacks = await GetRemainingPagesForType(firstPageHref, accessToken, new List<Buildpack>());

            return visibleBuildpacks;
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

            var stopAppPath = ListAppsPath + $"/{appGuid}/actions/stop";

            var uri = new UriBuilder(cfApiAddress)
            {
                Path = stopAppPath,
            };

            var request = new HttpRequestMessage(HttpMethod.Post, uri.ToString());
            request.Headers.Add("Authorization", "Bearer " + accessToken);

            var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"Response from POST `{stopAppPath}` was {response.StatusCode}");
            }

            string resultContent = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<App>(resultContent);

            if (result.State == "STOPPED")
            {
                return true;
            }

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

            var startAppPath = ListAppsPath + $"/{appGuid}/actions/start";

            var uri = new UriBuilder(cfApiAddress)
            {
                Path = startAppPath,
            };

            var request = new HttpRequestMessage(HttpMethod.Post, uri.ToString());
            request.Headers.Add("Authorization", "Bearer " + accessToken);

            var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"Response from POST `{startAppPath}` was {response.StatusCode}");
            }

            string resultContent = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<App>(resultContent);

            if (result.State == "STARTED")
            {
                return true;
            }

            return false;
        }

        public async Task<bool> DeleteAppWithGuid(string cfTarget, string accessToken, string appGuid)
        {
            // trust any certificate
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            ServicePointManager.ServerCertificateValidationCallback +=
                (sender, cert, chain, sslPolicyErrors) => { return true; };

            var deleteAppPath = DeleteAppsPath + $"/{appGuid}";

            var uri = new UriBuilder(cfTarget)
            {
                Path = deleteAppPath,
            };

            var request = new HttpRequestMessage(HttpMethod.Delete, uri.ToString());
            request.Headers.Add("Authorization", "Bearer " + accessToken);

            var response = await _httpClient.SendAsync(request);
            if (response.StatusCode != HttpStatusCode.Accepted)
            {
                throw new Exception($"Response from DELETE `{deleteAppPath}` was {response.StatusCode}");
            }

            if (response.StatusCode == HttpStatusCode.Accepted)
            {
                return true;
            }

            return false;
        }

        public async Task<List<Stack>> ListStacks(string cfTarget, string accessToken)
        {
            // trust any certificate
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            ServicePointManager.ServerCertificateValidationCallback +=
                (sender, cert, chain, sslPolicyErrors) => { return true; };

            var uri = new UriBuilder(cfTarget)
            {
                Path = ListStacksPath,
            };

            HypertextReference firstPageHref = new HypertextReference() { Href = uri.ToString() };

            return await GetRemainingPagesForType(firstPageHref, accessToken, new List<Stack>());
        }

        public async Task<LoginInfoResponse> GetLoginServerInformation(string cfApiAddress)
        {
            var loginServerUri = await GetAuthServerUriFromCfTarget(cfApiAddress);

            var uri = new UriBuilder(loginServerUri)
            {
                Path = LoginInfoPath,
            };

            var request = new HttpRequestMessage(HttpMethod.Get, uri.ToString());
            request.Headers.Add("Accept", "application/json");

            var response = await _httpClient.SendAsync(request);

            if (response.StatusCode != HttpStatusCode.OK)
            {
                throw new Exception($"Request for login server information was unsuccessful; request to {request.Method} {request.RequestUri} received {response.StatusCode}");
            }

            var jsonContent = await response.Content.ReadAsStringAsync();

            var deserializedResponse = JsonConvert.DeserializeObject<LoginInfoResponse>(jsonContent);

            return deserializedResponse;
        }

        private async Task<Uri> GetAuthServerUriFromCfTarget(string cfApiAddress)
        {
            try
            {
                Uri authServerUri = null;

                var uri = new UriBuilder(cfApiAddress)
                {
                    Path = "/",
                    Port = -1,
                };

                var request = new HttpRequestMessage(HttpMethod.Get, uri.ToString());

                var response = await _httpClient.SendAsync(request);

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var basicInfo = JsonConvert.DeserializeObject<BasicInfoResponse>(content);
                    authServerUri = new Uri(basicInfo.Links.Login.Href);
                }

                return authServerUri;
            }
            catch (Exception e)
            {
                e.Data.Add("MessageToDisplay", AuthServerLookupFailureMessage);
                throw;
            }
        }

        private Uri ValidateUriStringOrThrow(string uriString, string errorMessage)
        {
            Uri uriResult;
            Uri.TryCreate(uriString, UriKind.Absolute, out uriResult);

            if (uriResult == null)
            {
                throw new Exception(errorMessage);
            }

            return uriResult;
        }

        private async Task<List<TResourceType>> GetRemainingPagesForType<TResourceType>(HypertextReference pageAddress, string accessToken, List<TResourceType> resultsSoFar)
        {
            if (pageAddress == null)
            {
                return resultsSoFar;
            }

            var request = new HttpRequestMessage(HttpMethod.Get, pageAddress.Href);
            request.Headers.Add("Authorization", "Bearer " + accessToken);

            var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"Response from GET `{pageAddress}` was {response.StatusCode}");
            }

            string resultContent = await response.Content.ReadAsStringAsync();

            HypertextReference nextPageHref;

            if (typeof(TResourceType) == typeof(Org))
            {
                var results = JsonConvert.DeserializeObject<OrgsResponse>(resultContent);
                resultsSoFar.AddRange((IEnumerable<TResourceType>)results.Orgs.ToList());

                nextPageHref = results.Pagination.Next;
            }
            else if (typeof(TResourceType) == typeof(Space))
            {
                var results = JsonConvert.DeserializeObject<SpacesResponse>(resultContent);
                resultsSoFar.AddRange((IEnumerable<TResourceType>)results.Spaces.ToList());

                nextPageHref = results.Pagination.Next;
            }
            else if (typeof(TResourceType) == typeof(App))
            {
                var results = JsonConvert.DeserializeObject<AppsResponse>(resultContent);
                resultsSoFar.AddRange((IEnumerable<TResourceType>)results.Apps.ToList());

                nextPageHref = results.Pagination.Next;
            }
            else if (typeof(TResourceType) == typeof(Buildpack))
            {
                var results = JsonConvert.DeserializeObject<BuildpacksResponse>(resultContent);
                resultsSoFar.AddRange((IEnumerable<TResourceType>)results.Buildpacks.ToList());

                nextPageHref = results.Pagination.Next;
            }
            else if (typeof(TResourceType) == typeof(Stack))
            {
                var results = JsonConvert.DeserializeObject<StacksResponse>(resultContent);
                resultsSoFar.AddRange((IEnumerable<TResourceType>)results.Stacks.ToList());

                nextPageHref = results.Pagination.Next;
            }
            else
            {
                throw new Exception($"ResourceType unknown: {typeof(TResourceType).Name}");
            }

            return await GetRemainingPagesForType(nextPageHref, accessToken, resultsSoFar);
        }
    }
}
