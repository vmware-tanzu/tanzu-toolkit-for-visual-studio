using System.Collections.Generic;
using System.Threading.Tasks;
using Tanzu.Toolkit.CloudFoundryApiClient.Models.AppsResponse;
using Tanzu.Toolkit.CloudFoundryApiClient.Models.OrgsResponse;
using Tanzu.Toolkit.CloudFoundryApiClient.Models.SpacesResponse;

namespace Tanzu.Toolkit.CloudFoundryApiClient
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

        Task<bool> StartAppWithGuid(string cfTarget, string accessToken, string appGuid);

        Task<bool> DeleteAppWithGuid(string cfTarget, string accessToken, string appGuid);
    }
}
