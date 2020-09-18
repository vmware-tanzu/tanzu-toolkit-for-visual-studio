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

        public async Task LoginAsync(string cfTarget, string cfUsername, string cfPassword)
        {
            preventSsl(cfTarget);
            validateUriStringOrThrow(cfTarget, "Invalid target URI");
            var uaaUri = GetUaaUriFromCfTarget(cfTarget);

            var defaultUaaClientId = "cf";
            var defaultUaaClientSecret = "";

            int result = await _uaaClient.RequestAccessTokenAsync(uaaUri,
                                                                  defaultUaaClientId,
                                                                  defaultUaaClientSecret,
                                                                  cfUsername,
                                                                  cfPassword);

            if (result == (int)HttpStatusCode.OK) AccessToken = _uaaClient.Token.access_token;
        }

        private void preventSsl(string cfTarget)
        {
            if (cfTarget.StartsWith("https://"))
            {
                throw new Exception("SSL connections not supported; please use \"http://...\"");
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
            bool validCfUri = Uri.TryCreate(uriString, UriKind.Absolute, out uriResult)
                && uriResult.Scheme == Uri.UriSchemeHttp;

            if (!validCfUri) throw new Exception(errorMessage);

            return uriResult;
        }
    }
}
