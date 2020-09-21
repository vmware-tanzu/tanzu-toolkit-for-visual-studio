using System;
using System.Net;
using System.Threading.Tasks;

namespace TanzuForVS.CloudFoundryApiClient
{
    public class CfApiClient : ICfApiClient
    {
        private static IUaaClient _uaaClient;
        public string AccessToken { get; private set; }

        public CfApiClient(IUaaClient uaaClient)
        {
            _uaaClient = uaaClient;
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
            validateUriStringOrThrow(cfTarget, "Invalid target URI");
            var uaaUri = GetUaaUriFromCfTarget(cfTarget);

            var defaultUaaClientId = "cf";
            var defaultUaaClientSecret = "";

            var result = await _uaaClient.RequestAccessTokenAsync(uaaUri,
                                                                  defaultUaaClientId,
                                                                  defaultUaaClientSecret,
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

        private Uri GetUaaUriFromCfTarget(string cfTargetString)
        {
            var cfTargetUri = new Uri(cfTargetString);

            var uriHostMinusSubdomain = cfTargetUri.Host.Substring(cfTargetUri.Host.IndexOf('.'));
            var uaaHost = "uaa" + uriHostMinusSubdomain;
            var uaaTargetString = cfTargetUri.Scheme + "://" + uaaHost;

            Uri uaaTargetUri = validateUriStringOrThrow(uaaTargetString, "Unable to produce UAA URI");

            return uaaTargetUri;
        }

        private Uri validateUriStringOrThrow(string uriString, string errorMessage)
        {
            Uri uriResult;
            Uri.TryCreate(uriString, UriKind.Absolute, out uriResult);

            if (uriResult == null) throw new Exception(errorMessage);

            return uriResult;
        }
    }
}
