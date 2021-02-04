using System;
using System.Net;
using System.Threading.Tasks;
using Tanzu.Toolkit.CloudFoundryApiClient.Models.Token;

namespace Tanzu.Toolkit.CloudFoundryApiClient
{
    public interface IUaaClient
    {
        Token Token { get; }

        Task<HttpStatusCode> RequestAccessTokenAsync(Uri uaaUri, string uaaClientId, string uaaClientSecret, string cfUsername, string cfPassword);
    }
}