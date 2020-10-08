using System.Collections.Generic;
using System.Threading.Tasks;
using TanzuForVS.CloudFoundryApiClient.Models.OrgsResponse;

namespace TanzuForVS.CloudFoundryApiClient
{
    public interface ICfApiClient
    {
        string AccessToken { get; }

        Task<string> LoginAsync(string cfTarget, string cfUsername, string cfPassword);

        Task<List<Resource>> ListOrgs(string accessToken);
    }
}
