using System.Collections.Generic;
using System.Threading.Tasks;

namespace TanzuForVS.CloudFoundryApiClient
{
    public interface ICfApiClient
    {
        string AccessToken { get; }

        Task<string> LoginAsync(string cfTarget, string cfUsername, string cfPassword);

        Task<List<Models.OrgsResponse.Resource>> ListOrgs(string cfTarget, string accessToken);

        Task<List<Models.SpacesResponse.Resource>> ListSpaces(string cfTarget, string accessToken);
    }
}
