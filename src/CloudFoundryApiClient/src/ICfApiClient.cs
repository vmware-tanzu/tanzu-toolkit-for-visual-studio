using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Tanzu.Toolkit.CloudFoundryApiClient.Models;
using Tanzu.Toolkit.CloudFoundryApiClient.Models.AppsResponse;
using Tanzu.Toolkit.CloudFoundryApiClient.Models.OrgsResponse;
using Tanzu.Toolkit.CloudFoundryApiClient.Models.SpacesResponse;
using Tanzu.Toolkit.CloudFoundryApiClient.Models.StacksResponse;

namespace Tanzu.Toolkit.CloudFoundryApiClient
{
    public interface ICfApiClient
    {
        string AccessToken { get; }
        Task<List<Org>> ListOrgs(string cfTarget, string accessToken);
        Task<List<Space>> ListSpacesForOrg(string cfTarget, string accessToken, string orgGuid);
        Task<List<App>> ListAppsForSpace(string cfTarget, string accessToken, string spaceGuid);
        Task<bool> StopAppWithGuid(string cfTarget, string accessToken, string appGuid);
        Task<bool> StartAppWithGuid(string cfTarget, string accessToken, string appGuid);
        Task<bool> DeleteAppWithGuid(string cfTarget, string accessToken, string appGuid);
        Task<List<Buildpack>> ListBuildpacks(string cfApiAddress, string accessToken);
        Task<List<Stack>> ListStacks(string cfTarget, string accessToken);
        Task<LoginInfoResponse> GetLoginServerInformation(string cfApiAddress, bool trustAllCerts = false);
        Task<List<Route>> ListRoutesForApp(string cfTarget, string accessToken, string appGuid);
        Task<bool> DeleteRouteWithGuid(string cfTarget, string accessToken, string routeGuid);
        void Configure(Uri cfApiAddress, bool skipSslCertValidation);
        Task<List<App>> ListAppsAsync(string accessToken);
    }
}
