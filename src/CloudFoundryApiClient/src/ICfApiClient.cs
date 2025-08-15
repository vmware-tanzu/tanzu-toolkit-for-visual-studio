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

        Task<List<Org>> ListOrgsAsync(string cfTarget, string accessToken);

        Task<List<Space>> ListSpacesForOrgAsync(string cfTarget, string accessToken, string orgGuid);

        Task<List<App>> ListAppsForSpaceAsync(string cfTarget, string accessToken, string spaceGuid);

        Task<bool> StopAppWithGuidAsync(string cfTarget, string accessToken, string appGuid);

        Task<bool> StartAppWithGuidAsync(string cfTarget, string accessToken, string appGuid);

        Task<bool> DeleteAppWithGuidAsync(string cfTarget, string accessToken, string appGuid);

        Task<List<Buildpack>> ListBuildpacksAsync(string cfApiAddress, string accessToken);

        Task<List<Stack>> ListStacksAsync(string cfTarget, string accessToken);

        Task<LoginInfoResponse> GetLoginServerInformationAsync(string cfApiAddress, bool trustAllCerts = false);

        Task<List<Route>> ListRoutesForAppAsync(string cfTarget, string accessToken, string appGuid);

        Task<bool> DeleteRouteWithGuidAsync(string cfTarget, string accessToken, string routeGuid);

        void Configure(Uri cfApiAddress, bool skipSslCertValidation);

        Task<List<Service>> ListServicesAsync(string cfApiAddress, string accessToken);

        Task<List<App>> ListAppsAsyncAsync(string accessToken);
    }
}