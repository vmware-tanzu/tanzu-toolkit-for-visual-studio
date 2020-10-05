using System;
using System.Net.Http;
using System.Security;
using System.Threading.Tasks;
using TanzuForVS.CloudFoundryApiClient;

namespace TanzuForVS.Services.CloudFoundry
{
    public class CloudFoundryService : ICloudFoundryService
    {
        private static HttpClient _httpClient;

        public CloudFoundryService()
        {
            _httpClient = new HttpClient();
        }

        public bool IsLoggedIn { get; set; } = false;

        public async Task<ConnectResult> ConnectToCFAsync(string target, string username, SecureString password, string httpProxy, bool skipSsl)
        {
            if (string.IsNullOrEmpty(target))
            {
                throw new ArgumentException(nameof(target));
            }

            if (string.IsNullOrEmpty(username))
            {
                throw new ArgumentException(nameof(username));
            }

            if (password == null)
            {
                throw new ArgumentNullException(nameof(password));
            }

            var UaaClient = new UaaClient(_httpClient);
            var CfApiClient = new CfApiClient(UaaClient, _httpClient);

            try
            {
                // TODO: don't let password be passed around as a regular string
                // TODO: test that errors that may have been thrown in CfApiClient get passed
                //       through to this level & get loaded into the ConnectResult.ErrorMessage
                string AccessToken = await CfApiClient.LoginAsync(target, username, password.ToString());

                if (!string.IsNullOrEmpty(AccessToken)) return new ConnectResult(true, null);
                throw new Exception("Unable to login");
            }
            catch (Exception e)
            {
                return new ConnectResult(false, e.Message);
            }
        }
    }
}
