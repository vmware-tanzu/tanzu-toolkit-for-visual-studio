using System.Collections.Generic;
using System.Threading.Tasks;
using TanzuForVS.CloudFoundryApiClient.Models.AppsResponse;
using TanzuForVS.CloudFoundryApiClient.Models.OrgsResponse;
using TanzuForVS.CloudFoundryApiClient.Models.SpacesResponse;

namespace TanzuForVS.CloudFoundryApiClient
{
    public interface ICfApiClient
    {
        string AccessToken { get; }

        Task<string> LoginAsync(string cfTarget, string cfUsername, string cfPassword);

        Task<List<Org>> ListOrgs(string cfTarget, string accessToken);

        Task<List<Space>> ListSpaces(string cfTarget, string accessToken);

        Task<List<Space>> ListSpacesForOrg(string cfTarget, string accessToken, string orgGuid);

        Task<List<App>> ListAppsForSpace(string cfTarget, string accessToken, string spaceGuid);

        Task<bool> StopAppWithGuid(string cfTarget, string accessToken, string appGuid);
    }
}
