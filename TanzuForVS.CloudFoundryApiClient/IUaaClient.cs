using System.Threading.Tasks;
using System;

namespace TanzuForVS.CloudFoundryApiClient
{
    public interface IUaaClient
    {
        Token Token { get; }

        Task<int> RequestAccessTokenAsync(Uri uaaUri, string uaaClientId, string uaaClientSecret, string cfUsername, string cfPassword);
    }
}