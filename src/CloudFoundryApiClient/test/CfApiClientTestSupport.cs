using Newtonsoft.Json;
using RichardSzalay.MockHttp;
using System;
using System.Net.Http;
using Tanzu.Toolkit.CloudFoundryApiClient.Models;
using Tanzu.Toolkit.CloudFoundryApiClient.Models.AppsResponse;
using Tanzu.Toolkit.CloudFoundryApiClient.Models.BasicInfoResponse;
using Tanzu.Toolkit.CloudFoundryApiClient.Models.OrgsResponse;
using Tanzu.Toolkit.CloudFoundryApiClient.Models.SpacesResponse;
using Tanzu.Toolkit.CloudFoundryApiClient.Models.StacksResponse;

namespace Tanzu.Toolkit.CloudFoundryApiClient.Tests
{
    public class CfApiClientTestSupport
    {
        internal static readonly string _fakeTargetDomain = "myfaketarget.com";
        internal static readonly string _fakeCfApiAddress = $"https://api.{_fakeTargetDomain}";
        internal static readonly string _fakeLoginAddress = $"https://login.{_fakeTargetDomain}";
        internal static readonly string _fakeUaaAddress = $"https://uaa.{_fakeTargetDomain}";
        internal static readonly string _fakeCfUsername = "user";
        internal static readonly string _fakeCfPassword = "pass";
        internal static readonly string _fakeAccessToken = "fakeToken";

        internal static IHttpClientFactory _fakeHttpClientFactory = new FakeHttpClientFactory();

        internal static readonly string _fakeBasicInfoJsonResponse = JsonConvert.SerializeObject(new FakeBasicInfoResponse(
                loginHref: _fakeLoginAddress,
                uaaHref: _fakeUaaAddress));

        internal static readonly string _fakeOrgsJsonResponsePage1 = JsonConvert.SerializeObject(new FakeOrgsResponse(
            apiAddress: _fakeCfApiAddress,
            pageNum: 1,
            totalResults: 10,
            totalPages: 4,
            resultsPerPage: 3));

        internal static readonly string _fakeOrgsJsonResponsePage2 = JsonConvert.SerializeObject(new FakeOrgsResponse(
            apiAddress: _fakeCfApiAddress,
            pageNum: 2,
            totalResults: 10,
            totalPages: 4,
            resultsPerPage: 3));

        internal static readonly string _fakeOrgsJsonResponsePage3 = JsonConvert.SerializeObject(new FakeOrgsResponse(
            apiAddress: _fakeCfApiAddress,
            pageNum: 3,
            totalResults: 10,
            totalPages: 4,
            resultsPerPage: 3));

        internal static readonly string _fakeOrgsJsonResponsePage4 = JsonConvert.SerializeObject(new FakeOrgsResponse(
            apiAddress: _fakeCfApiAddress,
            pageNum: 4,
            totalResults: 10,
            totalPages: 4,
            resultsPerPage: 3));

        internal static readonly string _fakeSpacesJsonResponsePage1 = JsonConvert.SerializeObject(new FakeSpacesResponse(
            apiAddress: _fakeCfApiAddress,
            pageNum: 1,
            totalResults: 7,
            totalPages: 3,
            resultsPerPage: 3));

        internal static readonly string _fakeSpacesJsonResponsePage2 = JsonConvert.SerializeObject(new FakeSpacesResponse(
            apiAddress: _fakeCfApiAddress,
            pageNum: 2,
            totalResults: 7,
            totalPages: 3,
            resultsPerPage: 3));

        internal static readonly string _fakeSpacesJsonResponsePage3 = JsonConvert.SerializeObject(new FakeSpacesResponse(
            apiAddress: _fakeCfApiAddress,
            pageNum: 3,
            totalResults: 7,
            totalPages: 3,
            resultsPerPage: 3));

        internal static readonly string _fakeAppsJsonResponsePage1 = JsonConvert.SerializeObject(new FakeAppsResponse(
            apiAddress: _fakeCfApiAddress,
            pageNum: 1,
            totalResults: 125,
            totalPages: 3,
            resultsPerPage: 50));

        internal static readonly string _fakeAppsJsonResponsePage2 = JsonConvert.SerializeObject(new FakeAppsResponse(
            apiAddress: _fakeCfApiAddress,
            pageNum: 2,
            totalResults: 125,
            totalPages: 3,
            resultsPerPage: 50));

        internal static readonly string _fakeAppsJsonResponsePage3 = JsonConvert.SerializeObject(new FakeAppsResponse(
            apiAddress: _fakeCfApiAddress,
            pageNum: 3,
            totalResults: 125,
            totalPages: 3,
            resultsPerPage: 50));

        internal static readonly string _fakeBuildpacksJsonResponsePage1 = JsonConvert.SerializeObject(new FakeBuildpacksResponse(
            apiAddress: _fakeCfApiAddress,
            pageNum: 1,
            totalResults: 125,
            totalPages: 3,
            resultsPerPage: 50));

        internal static readonly string _fakeBuildpacksJsonResponsePage2 = JsonConvert.SerializeObject(new FakeBuildpacksResponse(
            apiAddress: _fakeCfApiAddress,
            pageNum: 2,
            totalResults: 125,
            totalPages: 3,
            resultsPerPage: 50));

        internal static readonly string _fakeBuildpacksJsonResponsePage3 = JsonConvert.SerializeObject(new FakeBuildpacksResponse(
            apiAddress: _fakeCfApiAddress,
            pageNum: 3,
            totalResults: 125,
            totalPages: 3,
            resultsPerPage: 50));

        internal static readonly string _fakeStacksJsonResponsePage1 = JsonConvert.SerializeObject(new FakeStacksResponse(
            apiAddress: _fakeCfApiAddress,
            pageNum: 1,
            totalResults: 10,
            totalPages: 4,
            resultsPerPage: 3));

        internal static readonly string _fakeStacksJsonResponsePage2 = JsonConvert.SerializeObject(new FakeStacksResponse(
            apiAddress: _fakeCfApiAddress,
            pageNum: 2,
            totalResults: 10,
            totalPages: 4,
            resultsPerPage: 3));

        internal static readonly string _fakeStacksJsonResponsePage3 = JsonConvert.SerializeObject(new FakeStacksResponse(
            apiAddress: _fakeCfApiAddress,
            pageNum: 3,
            totalResults: 10,
            totalPages: 4,
            resultsPerPage: 3));

        internal static readonly string _fakeStacksJsonResponsePage4 = JsonConvert.SerializeObject(new FakeStacksResponse(
            apiAddress: _fakeCfApiAddress,
            pageNum: 4,
            totalResults: 10,
            totalPages: 4,
            resultsPerPage: 3));

        internal static readonly string _fakeRoutesJsonResponsePage1 = JsonConvert.SerializeObject(new FakeRoutesResponse(
            apiAddress: _fakeCfApiAddress,
            pageNum: 1,
            totalResults: 125,
            totalPages: 3,
            resultsPerPage: 50));

        internal static readonly string _fakeRoutesJsonResponsePage2 = JsonConvert.SerializeObject(new FakeRoutesResponse(
            apiAddress: _fakeCfApiAddress,
            pageNum: 2,
            totalResults: 125,
            totalPages: 3,
            resultsPerPage: 50));

        internal static readonly string _fakeRoutesJsonResponsePage3 = JsonConvert.SerializeObject(new FakeRoutesResponse(
            apiAddress: _fakeCfApiAddress,
            pageNum: 3,
            totalResults: 125,
            totalPages: 3,
            resultsPerPage: 50));
    }

    internal class FakeBasicInfoResponse : BasicInfoResponse
    {
        public FakeBasicInfoResponse(string loginHref, string uaaHref) : base()
        {
            Links = new Models.BasicInfoResponse.Links
            {
                Login = new HypertextReference
                {
                    Href = loginHref,
                },
                Uaa = new HypertextReference
                {
                    Href = uaaHref,
                },
            };
        }
    }

    internal class FakeOrgsResponse : OrgsResponse
    {
        public FakeOrgsResponse(string apiAddress, int pageNum, int totalResults, int totalPages, int resultsPerPage) : base()
        {
            var isFirstPage = pageNum == 1;
            var isLastPage = pageNum == totalPages;

            var firstHref = new HypertextReference() { Href = $"{apiAddress}{CfApiClient.ListOrgsPath}?page=1&per_page={resultsPerPage}" };
            var lastHref = new HypertextReference() { Href = $"{apiAddress}{CfApiClient.ListOrgsPath}?page={totalPages}&per_page={resultsPerPage}" };
            var nextHref = isLastPage ? null : new HypertextReference() { Href = $"{apiAddress}{CfApiClient.ListOrgsPath}?page={pageNum + 1}&per_page={resultsPerPage}" };
            var previousHref = isFirstPage ? null : new HypertextReference() { Href = $"{apiAddress}{CfApiClient.ListOrgsPath}?page={pageNum - 1}&per_page={resultsPerPage}" };

            Pagination = new Pagination
            {
                Total_results = totalResults,
                Total_pages = totalPages,
                First = firstHref,
                Last = lastHref,
                Next = nextHref,
                Previous = previousHref,
            };

            Org[] orgs;
            if (isLastPage)
            {
                var numResourcesInLastPage = totalResults % resultsPerPage;
                orgs = new Org[numResourcesInLastPage];

                for (var i = 0; i < numResourcesInLastPage; i++)
                {
                    orgs[i] = new Org
                    {
                        Name = $"fakeOrg{i + 1}",
                        Guid = $"fakeOrgId-{i + 1}",
                    };
                }
            }
            else
            {
                orgs = new Org[resultsPerPage];

                for (var i = 0; i < resultsPerPage; i++)
                {
                    orgs[i] = new Org
                    {
                        Name = $"fakeOrg{i + 1}",
                        Guid = $"fakeOrgId-{i + 1}",
                    };
                }
            }

            Orgs = orgs;
        }
    }

    internal class FakeSpacesResponse : SpacesResponse
    {
        public FakeSpacesResponse(string apiAddress, int pageNum, int totalResults, int totalPages, int resultsPerPage) : base()
        {
            var isFirstPage = pageNum == 1;
            var isLastPage = pageNum == totalPages;

            var firstHref = new HypertextReference() { Href = $"{apiAddress}{CfApiClient.ListSpacesPath}?page=1&per_page={resultsPerPage}" };
            var lastHref = new HypertextReference() { Href = $"{apiAddress}{CfApiClient.ListSpacesPath}?page={totalPages}&per_page={resultsPerPage}" };
            var nextHref = isLastPage ? null : new HypertextReference() { Href = $"{apiAddress}{CfApiClient.ListSpacesPath}?page={pageNum + 1}&per_page={resultsPerPage}" };
            var previousHref = isFirstPage ? null : new HypertextReference() { Href = $"{apiAddress}{CfApiClient.ListSpacesPath}?page={pageNum - 1}&per_page={resultsPerPage}" };

            Pagination = new Pagination
            {
                Total_results = totalResults,
                Total_pages = totalPages,
                First = firstHref,
                Last = lastHref,
                Next = nextHref,
                Previous = previousHref,
            };

            Space[] spaces;
            if (isLastPage)
            {
                var numResourcesInLastPage = totalResults % resultsPerPage;
                spaces = new Space[numResourcesInLastPage];

                for (var i = 0; i < numResourcesInLastPage; i++)
                {
                    spaces[i] = new Space
                    {
                        Name = $"fakeSpace{i + 1}",
                        Guid = $"fakeSpaceId-{i + 1}",
                    };
                }
            }
            else
            {
                spaces = new Space[resultsPerPage];

                for (var i = 0; i < resultsPerPage; i++)
                {
                    spaces[i] = new Space
                    {
                        Name = $"fakeSpace{i + 1}",
                        Guid = $"fakeSpaceId-{i + 1}",
                    };
                }
            }

            Spaces = spaces;
        }
    }

    internal class FakeAppsResponse : AppsResponse
    {
        public FakeAppsResponse(string apiAddress, int pageNum, int totalResults, int totalPages, int resultsPerPage) : base()
        {
            var isFirstPage = pageNum == 1;
            var isLastPage = pageNum == totalPages;

            var firstHref = new HypertextReference() { Href = $"{apiAddress}{CfApiClient.ListAppsPath}?page=1&per_page={resultsPerPage}" };
            var lastHref = new HypertextReference() { Href = $"{apiAddress}{CfApiClient.ListAppsPath}?page={totalPages}&per_page={resultsPerPage}" };
            var nextHref = isLastPage ? null : new HypertextReference() { Href = $"{apiAddress}{CfApiClient.ListAppsPath}?page={pageNum + 1}&per_page={resultsPerPage}" };
            var previousHref = isFirstPage ? null : new HypertextReference() { Href = $"{apiAddress}{CfApiClient.ListAppsPath}?page={pageNum - 1}&per_page={resultsPerPage}" };

            Pagination = new Pagination
            {
                Total_results = totalResults,
                Total_pages = totalPages,
                First = firstHref,
                Last = lastHref,
                Next = nextHref,
                Previous = previousHref,
            };

            App[] apps;
            if (isLastPage)
            {
                var numResourcesInLastPage = totalResults % resultsPerPage;
                apps = new App[numResourcesInLastPage];

                for (var i = 0; i < numResourcesInLastPage; i++)
                {
                    apps[i] = new App
                    {
                        Name = $"fakeApp{i + 1}",
                    };
                }
            }
            else
            {
                apps = new App[resultsPerPage];

                for (var i = 0; i < resultsPerPage; i++)
                {
                    apps[i] = new App
                    {
                        Name = $"fakeApp{i + 1}",
                    };
                }
            }

            Apps = apps;
        }
    }

    internal class FakeBuildpacksResponse : BuildpacksResponse
    {
        public FakeBuildpacksResponse(string apiAddress, int pageNum, int totalResults, int totalPages, int resultsPerPage)
        {
            var isFirstPage = pageNum == 1;
            var isLastPage = pageNum == totalPages;

            var firstHref = new HypertextReference() { Href = $"{apiAddress}{CfApiClient.ListBuildpacksPath}?page=1&per_page={resultsPerPage}" };
            var lastHref = new HypertextReference() { Href = $"{apiAddress}{CfApiClient.ListBuildpacksPath}?page={totalPages}&per_page={resultsPerPage}" };
            var nextHref = isLastPage ? null : new HypertextReference() { Href = $"{apiAddress}{CfApiClient.ListBuildpacksPath}?page={pageNum + 1}&per_page={resultsPerPage}" };
            var previousHref = isFirstPage ? null : new HypertextReference() { Href = $"{apiAddress}{CfApiClient.ListBuildpacksPath}?page={pageNum - 1}&per_page={resultsPerPage}" };

            Pagination = new Pagination
            {
                Total_results = totalResults,
                Total_pages = totalPages,
                First = firstHref,
                Last = lastHref,
                Next = nextHref,
                Previous = previousHref,
            };

            Buildpack[] buildpacks;

            var numPreviousResults = (pageNum - 1) * resultsPerPage;
            var numStackTypesPerBuildpack = 3;
            /* INTENTION: assign Buildpacks prop to contain a list like this:
             * bp.Name = fakeBuildpack1, bp.Stack = fakeStack1
             * bp.Name = fakeBuildpack1, bp.Stack = fakeStack2
             * bp.Name = fakeBuildpack1, bp.Stack = fakeStack3
             * bp.Name = fakeBuildpack2, bp.Stack = fakeStack1
             * bp.Name = fakeBuildpack2, bp.Stack = fakeStack2
             * bp.Name = fakeBuildpack2, bp.Stack = fakeStack3
             * bp.Name = fakeBuildpack3, bp.Stack = fakeStack1
             * ...
             */

            if (isLastPage)
            {
                var numResourcesInLastPage = totalResults % resultsPerPage;
                buildpacks = new Buildpack[numResourcesInLastPage];

                for (var i = 0; i < numResourcesInLastPage; i++)
                {
                    var buildpackId = i / numStackTypesPerBuildpack + 1 + numPreviousResults;
                    var stackTypeId = i % numStackTypesPerBuildpack + 1 + numPreviousResults;

                    buildpacks[i] = new Buildpack
                    {
                        Name = $"fakeBuildpack{buildpackId}",
                        Stack = $"fakeStack{stackTypeId}",
                    };
                }
            }
            else
            {
                buildpacks = new Buildpack[resultsPerPage];

                for (var i = 0; i < resultsPerPage; i++)
                {
                    var buildpackId = i / numStackTypesPerBuildpack + 1 + numPreviousResults;
                    var stackTypeId = i % numStackTypesPerBuildpack + 1 + numPreviousResults;

                    buildpacks[i] = new Buildpack
                    {
                        Name = $"fakeBuildpack{buildpackId}",
                        Stack = $"fakeStack{stackTypeId}",
                    };
                }
            }

            Buildpacks = buildpacks;
        }
    }

    internal class FakeStacksResponse : StacksResponse
    {
        public FakeStacksResponse(string apiAddress, int pageNum, int totalResults, int totalPages, int resultsPerPage) : base()
        {
            var isFirstPage = pageNum == 1;
            var isLastPage = pageNum == totalPages;

            var firstHref = new HypertextReference() { Href = $"{apiAddress}{CfApiClient.ListStacksPath}?page=1&per_page={resultsPerPage}" };
            var lastHref = new HypertextReference() { Href = $"{apiAddress}{CfApiClient.ListStacksPath}?page={totalPages}&per_page={resultsPerPage}" };
            var nextHref = isLastPage ? null : new HypertextReference() { Href = $"{apiAddress}{CfApiClient.ListStacksPath}?page={pageNum + 1}&per_page={resultsPerPage}" };
            var previousHref = isFirstPage ? null : new HypertextReference() { Href = $"{apiAddress}{CfApiClient.ListStacksPath}?page={pageNum - 1}&per_page={resultsPerPage}" };

            Pagination = new Pagination
            {
                Total_results = totalResults,
                Total_pages = totalPages,
                First = firstHref,
                Last = lastHref,
                Next = nextHref,
                Previous = previousHref,
            };

            Stack[] stacks;
            if (isLastPage)
            {
                var numResourcesInLastPage = totalResults % resultsPerPage;
                stacks = new Stack[numResourcesInLastPage];

                for (var i = 0; i < numResourcesInLastPage; i++)
                {
                    stacks[i] = new Stack
                    {
                        Name = $"fakeStack{i + 1}",
                        Guid = $"fakeStackId-{i + 1}",
                    };
                }
            }
            else
            {
                stacks = new Stack[resultsPerPage];

                for (var i = 0; i < resultsPerPage; i++)
                {
                    stacks[i] = new Stack
                    {
                        Name = $"fakeStack{i + 1}",
                        Guid = $"fakeStackId-{i + 1}",
                    };
                }
            }

            Stacks = stacks;
        }
    }

    internal class FakeRoutesResponse : RoutesResponse
    {
        public FakeRoutesResponse(string apiAddress, int pageNum, int totalResults, int totalPages, int resultsPerPage) : base()
        {
            var isFirstPage = pageNum == 1;
            var isLastPage = pageNum == totalPages;

            var firstHref = new HypertextReference() { Href = $"{apiAddress}{CfApiClient.ListRoutesPath}?page=1&per_page={resultsPerPage}" };
            var lastHref = new HypertextReference() { Href = $"{apiAddress}{CfApiClient.ListRoutesPath}?page={totalPages}&per_page={resultsPerPage}" };
            var nextHref = isLastPage ? null : new HypertextReference() { Href = $"{apiAddress}{CfApiClient.ListRoutesPath}?page={pageNum + 1}&per_page={resultsPerPage}" };
            var previousHref = isFirstPage ? null : new HypertextReference() { Href = $"{apiAddress}{CfApiClient.ListRoutesPath}?page={pageNum - 1}&per_page={resultsPerPage}" };

            Pagination = new Pagination
            {
                Total_results = totalResults,
                Total_pages = totalPages,
                First = firstHref,
                Last = lastHref,
                Next = nextHref,
                Previous = previousHref,
            };

            Route[] routes;
            if (isLastPage)
            {
                var numResourcesInLastPage = totalResults % resultsPerPage;
                routes = new Route[numResourcesInLastPage];

                for (var i = 0; i < numResourcesInLastPage; i++)
                {
                    routes[i] = new Route
                    {
                        Guid = $"fakeRouteId-{i + 1}",
                    };
                }
            }
            else
            {
                routes = new Route[resultsPerPage];

                for (var i = 0; i < resultsPerPage; i++)
                {
                    routes[i] = new Route
                    {
                        Guid = $"fakeRouteId-{i + 1}",
                    };
                }
            }

            Routes = routes;
        }
    }

    public interface IFakeHttpClientFactory
    {
        MockHttpMessageHandler MockHttpMessageHandler { get; }

        HttpClient CreateClient(string name);
    }

    public class FakeHttpClientFactory : IHttpClientFactory, IFakeHttpClientFactory
    {
        public FakeHttpClientFactory()
        {
            MockHttpMessageHandler = new MockHttpMessageHandler();
            MockHttpMessageHandler.Fallback.Throw(new InvalidOperationException("No matching mock handler"));
        }

        public MockHttpMessageHandler MockHttpMessageHandler { get; private set; }

        public HttpClient CreateClient(string name)
        {
            return MockHttpMessageHandler.ToHttpClient();
        }
    }
}
