using System;
using System.Security;
using System.Threading.Tasks;

namespace TanzuForVS.Services.CloudFoundry
{
    public class CloudFoundryService : ICloudFoundryService
    {
 
        public bool IsLoggedIn { get; set; } = false;

        public Task<ConnectResult> ConnectToCFAsync(string target, string username, SecureString password, string httpProxy, bool skipSsl)
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

            return Task.FromResult(new ConnectResult(false, string.Empty));

        }
    }
}
